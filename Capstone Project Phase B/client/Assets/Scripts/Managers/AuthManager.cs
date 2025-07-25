using System; // Provides base class functionality
using System.Collections; // Enables use of IEnumerator and coroutines
using LoM.Super; // Project-specific base class for Unity MonoBehaviour
using TMPro; // TextMeshPro UI system
using UnityEngine; // Unity engine core features
using UnityEngine.Networking; // UnityWebRequest API for HTTP requests
using UnityEngine.UI; // UI system in Unity

public class AuthManager : SuperBehaviour {
    // Manages login, registration, and secure credential handling
    private const string UsernameKey = "SavedUsername"; // Key to store username in PlayerPrefs
    private const string PasswordKey = "SavedPassword"; // Key to store password in PlayerPrefs

    [SerializeField] private Button exitButton; // Reference to the exit button

    [Header("Login Components")] [SerializeField]
    private TMP_InputField loginUsernameInput; // Input field for username during login

    [SerializeField] private TMP_InputField loginPasswordInput; // Input field for password during login
    [SerializeField] private TextMeshProUGUI loginMessageText; // Text field for login feedback messages

    [Header("Register Components")] [SerializeField]
    private TMP_InputField registerEmailInput; // Input field for email during registration

    [SerializeField] private TMP_InputField registerUsernameInput; // Input field for username during registration
    [SerializeField] private TMP_InputField registerPasswordInput; // Input field for password during registration
    [SerializeField] private RegisterInputsValidity registerInputsValidity;
    [SerializeField] private TextMeshProUGUI registerMessageText; // Text field for registration feedback messages

    private Coroutine _dotAnimationCoroutine; // Coroutine handler for the animated dots during loading
    private bool _isNewRegistration; // Flag to check if the user is registering or logging in
    private string _savedUsername; // Stores the previously saved username from PlayerPrefs
    private string _savedDecryptedPassword; // Stores the decrypted password from PlayerPrefs

    private void Awake() {
        exitButton.onClick.AddListener(Application.Quit); // Assign quit action to exit button
        LoadSavedCredentials(); // Attempt to preload saved credentials if they exist
    }

    private void LoadSavedCredentials() {
        // Attempts to load previously saved credentials from PlayerPrefs
        if (!PlayerPrefs.HasKey(UsernameKey) || !PlayerPrefs.HasKey(PasswordKey)) return; // Exit if not found

        string savedUsernameTemp = PlayerPrefs.GetString(UsernameKey); // Load stored username
        string encryptedPassword = PlayerPrefs.GetString(PasswordKey); // Load stored encrypted password

        if (string.IsNullOrEmpty(savedUsernameTemp) || string.IsNullOrEmpty(encryptedPassword)) {
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.DeleteKey(PasswordKey);
            PlayerPrefs.Save();
            _savedUsername = null;
            _savedDecryptedPassword = null;
            return;
        }

        try {
            string decryptedPassword =
                EncryptionHelper.Decrypt(encryptedPassword, savedUsernameTemp); // Attempt decryption
            _savedUsername = savedUsernameTemp;
            _savedDecryptedPassword = decryptedPassword;

            loginUsernameInput.text = _savedUsername; // Auto fill username field
            loginPasswordInput.text = _savedDecryptedPassword; // Auto fill password field
        } catch (Exception) {
            PlayerPrefs.DeleteKey(UsernameKey); // Clear invalid data
            PlayerPrefs.DeleteKey(PasswordKey);
            PlayerPrefs.Save();
            _savedUsername = null;
            _savedDecryptedPassword = null;
        }
    }

    // Checks if the entered credentials match saved credentials
    private bool AreCredentialsUnchanged(string username, string password) {
        if (string.IsNullOrEmpty(_savedUsername) || string.IsNullOrEmpty(_savedDecryptedPassword)) return false;
        return username == _savedUsername && password == _savedDecryptedPassword;
    }

    // Triggered when the login button is clicked
    public void OnLoginClicked() {
        string username = loginUsernameInput.text.Trim(); // Remove whitespace from username input
        string password = loginPasswordInput.text.Trim(); // Remove whitespace from password input

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
            loginMessageText.text = "Please fill in all fields.";
            return;
        }

        _isNewRegistration = false;
        StartCoroutine(Login(username, password)); // Begin login coroutine
    }

    // Triggered when the register button is clicked
    public void OnRegisterClicked() {
        string email = registerEmailInput.text.Trim();
        string username = registerUsernameInput.text.Trim();
        string password = registerPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
            registerMessageText.text = "Please fill in all fields.";
            return;
        }

        if (!registerInputsValidity.CheckAllInputsValidity()) {
            registerMessageText.text = "Please fill in all fields correctly.";
            return;
        }

        _isNewRegistration = true;
        StartCoroutine(Register(username, password, email)); // Begin registration coroutine
    }

    // Coroutine to handle login request to server
    private IEnumerator Login(string username, string password) {
        // Construct login URL
        loginMessageText.text = string.Empty;
        _dotAnimationCoroutine = StartCoroutine(DotAnimation(loginMessageText));
        // Build login URL
        string url = Constants.ServerEndpoints.LoginEndpoint(username, password);
        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest(); // Send request
        // Stop animation
        if (_dotAnimationCoroutine != null) {
            StopCoroutine(_dotAnimationCoroutine);
            _dotAnimationCoroutine = null;
        }

        if (request.result != UnityWebRequest.Result.Success) {
            // Parse and show error
            LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            loginMessageText.text = "Login failed: " + loginResponse.message;
        } else {
            // Parse and process successful login
            LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            if (loginResponse.token != null) {
                DataManager.Instance.SessionToken = loginResponse.token; // Store session token
                DataManager.Instance.LoggedInUsername = username;
                // Save credentials if changed
                if (!AreCredentialsUnchanged(username, password)) {
                    try {
                        loginMessageText.text = "Saving credentials...";
                        string encryptedPassword = EncryptionHelper.Encrypt(password, username); // Encrypt password
                        PlayerPrefs.SetString(UsernameKey, username);
                        PlayerPrefs.SetString(PasswordKey, encryptedPassword);
                        PlayerPrefs.Save();
                        _savedUsername = username;
                        _savedDecryptedPassword = password;
                    } catch (Exception) {
                        // ignored
                    }
                }

                loginMessageText.color = Color.black;
                loginMessageText.text = "Login successful!";
                GameManager.Instance.LoadGame(_isNewRegistration); // Continue to game
            } else {
                loginMessageText.color = Color.red;
                loginMessageText.text = loginResponse.message;
            }
        }
    }

    // Coroutine to handle registration request to server
    private IEnumerator Register(string username, string password, string email) {
        registerMessageText.text = string.Empty;
        _dotAnimationCoroutine = StartCoroutine(DotAnimation(registerMessageText));

        string url = Constants.ServerEndpoints.RegisterEndpoint(username, password, email);
        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (_dotAnimationCoroutine != null) {
            StopCoroutine(_dotAnimationCoroutine);
            _dotAnimationCoroutine = null;
        }

        if (request.result != UnityWebRequest.Result.Success) {
            registerMessageText.color = Color.red;
            registerMessageText.text = "Registration failed: " + request.downloadHandler.text;
        } else {
            registerMessageText.color = Color.black;
            registerMessageText.text = request.downloadHandler.text;
            yield return StartCoroutine(Login(username, password)); // Automatically login after register
        }
    }

    // Displays an animated loading sequence using dots
    private static IEnumerator DotAnimation(TextMeshProUGUI textField) {
        textField.color = Color.black;
        string[] dots = { ".", "..", "...", "....", ".....", "......" };
        int dotIndex = 0;
        while (true) {
            textField.text = dots[dotIndex];
            dotIndex = (dotIndex + 1) % dots.Length;
            yield return new WaitForSeconds(0.1f); // Delay between dot changes
        }
    }

    // Cleans up on destroy
    private void OnDestroy() {
        exitButton.onClick.AddListener(Application.Quit);
        if (_dotAnimationCoroutine != null) StopCoroutine(_dotAnimationCoroutine);
    }
}

// Represents a response returned from the login endpoint
[Serializable]
public class LoginResponse {
    public string token;// JWT or session token from server
    public string message;// Message response from server (error/success)
}