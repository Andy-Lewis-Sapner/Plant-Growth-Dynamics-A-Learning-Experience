using System.Text.RegularExpressions;
using LoM.Super;
using TMPro;
using UnityEngine;

public class RegisterInputsValidity : SuperBehaviour {
    private const string EmailRegex =
        "^(?(\")(\".+?(?<!\\\\)\"@)|(([0-9a-z]((\\.(?!\\.))|[-!#\\$%&'\\*\\+/=\\?\\^`\\{\\}\\|~\\w])*)(?<=[0-9a-z])@))(?(\\[)(\\[(\\d{1,3}\\.){3}\\d{1,3}\\])|(([0-9a-z][-\\w]*[0-9a-z]*\\.)+[a-z0-9][\\-a-z0-9]{0,22}[a-z0-9]))$";

    private const string UsernameRegex = "^[a-zA-Z0-9_]+$";
    private const string PasswordRegex = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

    [SerializeField] private TextMeshProUGUI registerMessageText;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField passwordInputField;

    /// <summary>
    /// Validates email input on change and displays appropriate validation messages.
    /// </summary>
    /// <param name="email">The input email string.</param>
    public void OnEmailInputChanged(string email) {
        email = email.Trim();
        if (string.IsNullOrEmpty(email))
            registerMessageText.text = string.Empty;
        else
            registerMessageText.text = !CheckInputValidity(email, EmailRegex)
                ? "Invalid email format."
                : string.Empty;
    }

    /// <summary>
    /// Validates username input on change and displays appropriate validation messages.
    /// </summary>
    /// <param name="username">The input username string.</param>
    public void OnUsernameInputChanged(string username) {
        username = username.Trim();
        if (string.IsNullOrEmpty(username))
            registerMessageText.text = string.Empty;
        else
            registerMessageText.text = !CheckInputValidity(username, UsernameRegex)
                ? "Username can only contain letters, numbers and underscores."
                : string.Empty;
    }

    /// <summary>
    /// Validates password input on change and displays appropriate validation messages.
    /// Password must be at least 8 characters long and contain one letter, one number, and one special character.
    /// </summary>
    /// <param name="password">The input password string.</param>
    public void OnPasswordInputChanged(string password) {
        password = password.Trim();
        if (string.IsNullOrEmpty(password))
            registerMessageText.text = string.Empty;
        else
            registerMessageText.text = !CheckInputValidity(password, PasswordRegex)
                ? "Password must be at least 8 characters long and contain at least one letter, one number, and one special character (@$!%*?&)."
                : string.Empty;
    }

    /// <summary>
    /// Checks if the input string matches the specified regular expression.
    /// </summary>
    private static bool CheckInputValidity(string input, string regex) {
        return Regex.IsMatch(input, regex);
    }

    /// <summary>
    /// Checks if all input fields have valid values.
    /// </summary>
    public bool CheckAllInputsValidity() {
        return CheckInputValidity(emailInputField.text, EmailRegex) &&
               CheckInputValidity(usernameInputField.text, UsernameRegex) &&
               CheckInputValidity(passwordInputField.text, PasswordRegex);
    }
}