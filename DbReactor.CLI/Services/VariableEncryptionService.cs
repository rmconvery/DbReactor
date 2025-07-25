using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class VariableEncryptionService : IVariableEncryptionService
{
    private readonly ILogger<VariableEncryptionService> _logger;
    private const string EncryptionPrefix = "ENC:";
    private const string KeySource = "DbReactor.CLI.Variables";

    public VariableEncryptionService(ILogger<VariableEncryptionService> logger)
    {
        _logger = logger;
    }

    public string EncryptValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        try
        {
            var key = DeriveKey();
            var iv = new byte[16]; // AES block size
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    // Prepend IV to encrypted data
                    msEncrypt.Write(iv, 0, iv.Length);
                    
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(value);
                    }
                    
                    var encryptedBytes = msEncrypt.ToArray();
                    return EncryptionPrefix + Convert.ToBase64String(encryptedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt variable value");
            throw;
        }
    }

    public string DecryptValue(string encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue) || !IsEncrypted(encryptedValue))
            return encryptedValue;

        try
        {
            var base64Data = encryptedValue.Substring(EncryptionPrefix.Length);
            var encryptedBytes = Convert.FromBase64String(base64Data);

            var key = DeriveKey();
            
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                
                // Extract IV from the beginning of encrypted data
                var iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                // Extract actual encrypted data (skip IV)
                var actualEncryptedData = new byte[encryptedBytes.Length - 16];
                Array.Copy(encryptedBytes, 16, actualEncryptedData, 0, actualEncryptedData.Length);

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(actualEncryptedData))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt variable value");
            throw new InvalidOperationException("Failed to decrypt variable value", ex);
        }
    }

    public bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptionPrefix);
    }

    public string MaskEncryptedValue(string encryptedValue)
    {
        if (!IsEncrypted(encryptedValue))
            return MaskPlainValue(encryptedValue);

        // Show that it's encrypted and mask the encrypted data
        var base64Part = encryptedValue.Substring(EncryptionPrefix.Length);
        if (base64Part.Length <= 8)
            return EncryptionPrefix + new string('*', base64Part.Length);
        
        return EncryptionPrefix + base64Part.Substring(0, 4) + new string('*', base64Part.Length - 8) + base64Part.Substring(base64Part.Length - 4);
    }

    private static string MaskPlainValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
            
        return value.Length <= 4 
            ? new string('*', value.Length)
            : value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2);
    }

    private byte[] DeriveKey()
    {
        // Use PBKDF2 to derive a key from machine-specific information
        // This ensures the key is unique per machine but deterministic
        var machineKey = Environment.MachineName + Environment.UserName + KeySource;
        
        using (var pbkdf2 = new Rfc2898DeriveBytes(
            machineKey, 
            Encoding.UTF8.GetBytes("DbReactor.Salt.V1"), // Static salt for consistency
            100000, // 100k iterations
            HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(32); // 256-bit key for AES
        }
    }
}