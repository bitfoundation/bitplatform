// C# implementation of the PDF standard security handler, following the
// algorithms in the PDF specification (ISO 32000, §7.6). Supports
// empty-user-password decryption for revisions 2–6 (RC4, AESV2/AES-128,
// AESV3/AES-256).

using System.Security.Cryptography;

// MD5/AES are used only for optional encrypted-PDF support and are available on
// server/desktop hosting. They are not present in the browser (WASM) sandbox,
// where encrypted documents are simply unsupported; suppress the platform
// analyzer rather than fail the build for all consumers.
#pragma warning disable CA1416

namespace Bit.BlazorUI;

internal enum BitPdfCipherKind { None, Rc4, Aes128, Aes256 }
