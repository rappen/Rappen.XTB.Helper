using System;
using System.Security.Cryptography;
using System.Text;

namespace Rappen.AI.WinForm
{
    /// <summary>
    /// Reusable machine/user-bound secret storage using Windows DPAPI
    /// (<see cref="ProtectedData"/> with <see cref="DataProtectionScope.CurrentUser"/>).
    /// Each caller supplies its own <paramref name="entropy"/> so different secrets (e.g. tokens
    /// for different OAuth providers) cannot be decrypted with each other's context.
    ///
    /// This is a DELIBERATE choice over XrmToolBox's connection-password scheme
    /// (McTools.Xrm.Connection's CryptoManager: Rijndael + an embedded passphrase, which is
    /// reversible by anyone with the assembly, because connection files are meant to be portable).
    /// For bearer tokens we want the opposite: the ciphertext is bound to the current Windows
    /// user + machine, there is no key material in the assembly, and a copied/roamed settings file
    /// simply fails to decrypt (Unprotect returns "") so the user is prompted to sign in again.
    /// </summary>
    public static class SecretProtector
    {
        /// <summary>Encrypts a secret for the current Windows user (DPAPI). Returns base64, or empty for null/empty input.</summary>
        /// <param name="plainText">The secret to protect.</param>
        /// <param name="entropy">Caller-specific additional entropy; must be identical for the matching Unprotect call.</param>
        public static string Protect(string plainText, byte[] entropy)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(bytes, entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>Decrypts a base64 secret produced by <see cref="Protect"/>. Returns empty on any failure (bad base64, wrong entropy, different user/machine).</summary>
        /// <param name="protectedBase64">The base64 value produced by <see cref="Protect"/>.</param>
        /// <param name="entropy">The same entropy that was passed to <see cref="Protect"/>.</param>
        public static string Unprotect(string protectedBase64, byte[] entropy)
        {
            if (string.IsNullOrEmpty(protectedBase64))
            {
                return string.Empty;
            }
            try
            {
                var encrypted = Convert.FromBase64String(protectedBase64);
                var bytes = ProtectedData.Unprotect(encrypted, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>Builds an entropy byte array from a stable per-secret label (e.g. a provider key + version).</summary>
        public static byte[] EntropyFromLabel(string label)
        {
            return Encoding.UTF8.GetBytes(label ?? string.Empty);
        }
    }
}
