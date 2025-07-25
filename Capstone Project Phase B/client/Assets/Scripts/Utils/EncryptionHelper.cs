using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Provides methods for AES-based string encryption and decryption using a username-derived key.
/// </summary>
public static class EncryptionHelper {
    private const string EncryptionSalt = "PlantingIsFunAndFunIsPlanting";
    private const int KeySize = 256;
    private const int Iterations = 100000;

    /// <summary>
    /// Encrypts the given plaintext using AES and a key derived from the username.
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <param name="username">The username used to derive the encryption key.</param>
    /// <returns>Base64-encoded encrypted string.</returns>
    public static string Encrypt(string plainText, string username) {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(username))
            throw new ArgumentException("Text or username cannot be null or empty.");

        byte[] salt = Encoding.UTF8.GetBytes(EncryptionSalt);
        byte[] key = new Rfc2898DeriveBytes(username, salt, Iterations, HashAlgorithmName.SHA256).GetBytes(KeySize / 8);
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        byte[] result = new byte[iv.Length + encryptedBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts the given Base64-encoded string using a key derived from the username.
    /// </summary>
    /// <param name="cipherText">The encrypted Base64 string.</param>
    /// <param name="username">The username used to derive the decryption key.</param>
    /// <returns>The decrypted plaintext.</returns>
    public static string Decrypt(string cipherText, string username) {
        if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(username))
            throw new ArgumentException("Text or username cannot be null or empty.");

        byte[] cipherBytes = Convert.FromBase64String(cipherText);
        byte[] salt = Encoding.UTF8.GetBytes(EncryptionSalt);
        byte[] key = new Rfc2898DeriveBytes(username, salt, Iterations, HashAlgorithmName.SHA256).GetBytes(KeySize / 8);

        using Aes aes = Aes.Create();
        aes.Key = key;
        byte[] iv = new byte[16];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        byte[] encryptedBytes = new byte[cipherBytes.Length - iv.Length];
        Buffer.BlockCopy(cipherBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}