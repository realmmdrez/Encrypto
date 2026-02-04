# Encrypto

Encrypto is a desktop application for performing **local cryptographic operations** in a simple, controlled, and offline-first environment. It is designed as a practical security-focused tool rather than a cloud service or convenience wrapper.

This project was developed as a **final university project** and is published publicly for learning, review, and experimentation.

---

## Features

- **File Encryption**
  - Hybrid encryption using RSA and AES-GCM
  - Encrypt files locally without network access

- **File Decryption**
  - Secure decryption using password-protected private keys

- **Key Generation**
  - RSA key pair generation
  - Private keys protected using modern password-based encryption

- **Secure Delete**
  - Best-effort secure file deletion through multi-pass overwriting  
  - Intended for risk reduction, not guaranteed forensic erasure

- **Offline & Local**
  - No telemetry
  - No cloud dependencies
  - No background services

---

## Security Model (High-Level)

- **Symmetric encryption:** AES-256-GCM  
- **Asymmetric encryption:** RSA (OAEP with SHA-256)  
- **Key derivation:** PBKDF2 (SHA-256, high iteration count)  

All cryptographic operations are performed locally using standard, well-known primitives.  
This project does **not** attempt to invent new cryptography.

---

## Important Notes

- This software is **not audited**
- It should **not** be used for protecting high-value or life-critical data
- Secure deletion on modern filesystems and SSDs is **best-effort only**
- The project prioritizes transparency and correctness over feature breadth

---

## Technology

- **Platform:** Windows
- **Framework:** WPF (.NET)
- **Language:** C#
- **Cryptography:** .NET cryptography libraries

---

## License

This project is released under the **MIT License**.  
You are free to use, modify, and distribute it, with no warranty provided.

---

## Disclaimer

This project is provided for educational purposes.  
The author makes no guarantees regarding security, fitness for purpose, or resistance to attack.
