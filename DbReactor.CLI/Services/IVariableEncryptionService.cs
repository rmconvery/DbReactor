namespace DbReactor.CLI.Services;

public interface IVariableEncryptionService
{
    string EncryptValue(string value);
    string DecryptValue(string encryptedValue);
    bool IsEncrypted(string value);
    string MaskEncryptedValue(string encryptedValue);
}