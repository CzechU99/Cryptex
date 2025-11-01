<h2 align=\"center\"><strong>Cryptex — Szyfrowanie i odszyfrowywanie plików (AES‑GCM / ChaCha20‑Poly1305)</strong></h2>

<div align=\"center\">
  <p>
    <img alt=\"Status\" src=\"https://img.shields.io/badge/status-active-0ea5e9\">
    <img alt=\"Licencja\" src=\"https://img.shields.io/badge/licencja-private-64748b\">
  </p>
  <p>
    <img alt=\".NET\" src=\"https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white\">
    <img alt=\"ASP.NET Core\" src=\"https://img.shields.io/badge/ASP.NET_Core-512BD4?logo=dotnet&logoColor=white\">
    <img alt=\"C#\" src=\"https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white\">
    <img alt=\"Vue\" src=\"https://img.shields.io/badge/Vue-3-42b883?logo=vuedotjs&logoColor=white\">
    <img alt=\"Vite\" src=\"https://img.shields.io/badge/Vite-7-646CFF?logo=vite&logoColor=white\">
    <img alt=\"Axios\" src=\"https://img.shields.io/badge/Axios-HTTP-5a29e4\">
  </p>
</div>

---

## Cel projektu

Cryptex to prosta i szybka aplikacja do bezpiecznego szyfrowania i odszyfrowywania plików. Back‑end oparty o ASP.NET Core udostępnia dwa endpointy API (encrypt/decrypt) z nowoczesnymi algorytmami AEAD (AES‑GCM oraz ChaCha20‑Poly1305). Front‑end w Vue 3 zapewnia nowoczesny interfejs (wybór pliku, generator hasła, pasek siły, log „konsola”, stylowane kontrolki).

---

## Technologie

- ASP.NET Core 8 (.NET 8) — REST API i logika kryptograficzna
- C# — implementacja AEAD, PBKDF2 (Rfc2898DeriveBytes), limit prób
- Vue 3 + Vite — front‑end (SPA), proxy do API w dev
- Axios/Fetch — wywołania API (multipart/form‑data)
- Swagger — dokumentacja API w dev

---

## Funkcje

- Szyfrowanie plików algorytmami: AES‑GCM lub ChaCha20‑Poly1305
- Deszyfrowanie z weryfikacją integralności (tag uwierzytelniający)
- Ochrona hasła: PBKDF2 (SHA‑256, iteracje) z solą
- Opcjonalny czas wygaśnięcia pliku (UTC ticks w nagłówku danych)
- Kontrola prób deszyfrowania i blokada pliku po limitach
- Frontend: generator „Losowe”, pasek siły hasła, stylowany select i file picker, log „konsola”, dostępnościowy układ pól (ukryte username)

---

## Struktura zaszyfrowanego pliku

Parametry (domyślne z pps/server/appsettings.json):

- SALT_SIZE = 16
- IV_SIZE = 12
- TAG_SIZE = 16
- HASH_SIZE = 32
- ITERATION_COUNT = 100000

Układ bajtów (bez czasu wygaśnięcia):

1. Algorithm (1 bajt)
   - 0 = AES‑GCM, 1 = ChaCha20‑Poly1305
2. Salt (16 bajtów)
3. IV/Nonce (12 bajtów)
4. Cipher || Tag (N + 16 bajtów)
   - ostatnie 16 bajtów tego segmentu to Tag
5. PasswordHash (32 bajty)
   - PBKDF2 (SHA‑256) z salt i ITERATION_COUNT

Układ z czasem wygaśnięcia (ExpireTime):

- Pozycja po wstawieniu: tuż po IV (offset 1 + SALT_SIZE + IV_SIZE = 29)
- Wstawiane bajty: Int64 ticks w UTC (8 bajtów, little‑endian)

Zatem przy wygaśnięciu: Algorithm | Salt | IV | ExpireTicks(8) | Cipher || Tag | PasswordHash

Logika:

- Szyfrowanie: Encrypt* konkatenuje Algorithm + Salt + IV + Cipher + Tag + PasswordHash, a FileService.CombineEncryptedData opcjonalnie wstawia 8 bajtów daty po IV.
- Deszyfrowanie: ExtractDetailsFromFile odczytuje Algorithm/Salt/IV/PasswordHash, a ExtractCipherTagAndDate wykrywa 8 bajtów czasu (próba parsowania DateTime ±100 lat). SplitEncryptedData dzieli Cipher || Tag (ostatnie 16 bajtów to tag).

---

## API (REST)

Base (dev): /api

- POST /api/File/encrypt — multipart/form‑data
  - File: plik (binarnie) [wymagane]
  - Password: string, min. 8 znaków [wymagane]
  - Algorithm: AES-GCM lub ChaCha20-Poly1305 [opcjonalne]
  - ExpireTime: ISO8601 (UTC) [opcjonalne]
  - Response: pplication/octet-stream — zwraca plik *.enc

- POST /api/File/decrypt — multipart/form‑data
  - File: plik *.enc [wymagane]
  - Password: string [wymagane]
  - Response: pplication/octet-stream — zwraca pierwotny plik

Przykłady curl:

`ash
curl -k -F "File=@/path/file.pdf" -F "Password=MoceHaslo123!" -F "Algorithm=AES-GCM" -F "ExpireTime=2026-01-01T12:00:00Z" https://localhost:7278/api/File/encrypt -o file.pdf.enc

curl -k -F "File=@file.pdf.enc" -F "Password=MoceHaslo123!" https://localhost:7278/api/File/decrypt -o file.pdf
`

Typowe błędy (400):

- Brak pliku / błędne rozszerzenie .enc
- Brak hasła / hasło krótsze niż MIN_PASSWORD_LENGTH
- Plik wygasł (po ExpireTime)
- Nieprawidłowy format pliku

---

## Uruchomienie (dev)

Back‑end (HTTPS):

`ash
cd apps/server
dotnet run --launch-profile https
# Swagger: https://localhost:7278/swagger
`

Front‑end:

`ash
cd apps/client
npm install
npm run dev
# Vite proxy -> https://localhost:7278 (secure:false)
`

Konfiguracja frontu:

- pps/client/.env
  - VITE_API_BASE_URL=/api — korzysta z Vite proxy w dev
- pps/client/vite.config.js
  - proxy: /api → https://localhost:7278 (bez weryfikacji certyfikatu dev)

---

## Struktura projektu

- pps/server
  - Controllers/api/FileController.cs — encrypt/decrypt
  - Services/* — EncryptionService, FileService, ValidationService, RateLimitService, ExpireTimeService
  - Models/FileModel.cs — EncryptRequest, DecryptRequest
  - Config/AppSettings.cs + ppsettings.json — rozmiary SALT/IV/TAG/HASH, iteracje itp.

- pps/client
  - src/components — EncryptForm, DecryptForm, PasswordField, FancySelect, FilePicker, ConsoleLog
  - src/api/fileService.js — pomocnicze wywołania API (FormData)
  - src/style.css — motyw, konsola, selekt, picker pliku

---

## Uwagi bezpieczeństwa

- Hasła nie są przechowywane; wyprowadzanie klucza przez PBKDF2 (SHA‑256, iteracje) opiera się na podanym haśle i soli.
- Tag AEAD (16 bajtów) weryfikuje integralność — każda modyfikacja danych zostanie wykryta (błąd uwierzytelnienia).
- Czas wygaśnięcia (jeśli ustawiony) jest walidowany po stronie serwera przed odszyfrowaniem.

---

## Licencja

Prywatna. Wszelkie prawa zastrzeżone.
