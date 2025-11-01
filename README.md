<h2 align="center"><strong>Cryptex — Szyfrowanie i odszyfrowywanie plików</strong></h2>

<div align="center">
  <p>
    <img alt="Status" src="https://img.shields.io/badge/status-aktywny-0ea5e9">
    <img alt="Licencja" src="https://img.shields.io/badge/licencja-prywatna-64748b">
  </p>
  <p>
    <img alt="C#" src="https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white">
    <img alt=".NET 8" src="https://img.shields.io/badge/.NET%208.0-512BD4?logo=dotnet&logoColor=white">
    <img alt="ASP.NET Core" src="https://img.shields.io/badge/ASP.NET%20Core-512BD4?logo=dotnet&logoColor=white">
    <img alt="Swagger" src="https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=white">
    <img alt="Vue 3" src="https://img.shields.io/badge/Vue.js-4FC08D?logo=vue.js&logoColor=white">
    <img alt="Vite" src="https://img.shields.io/badge/Vite-646CFF?logo=vite&logoColor=white">
    <img alt="Axios" src="https://img.shields.io/badge/Axios-5A29E4?logo=axios&logoColor=white">
    <img alt="JavaScript" src="https://img.shields.io/badge/JavaScript-F7DF1E?logo=javascript&logoColor=black">
    <img alt="REST API" src="https://img.shields.io/badge/REST%20API-009688?logo=swagger&logoColor=white">
  </p>
</div>

---

## 🎯 Cel projektu

**Cryptex** to lekka aplikacja do bezpiecznego szyfrowania i odszyfrowywania plików z wykorzystaniem nowoczesnych algorytmów AEAD — **AES-GCM** i **ChaCha20-Poly1305**.  
Projekt składa się z dwóch części:

- **Back-end (ASP.NET Core 8)** — udostępnia dwa endpointy REST API (`/encrypt` i `/decrypt`), realizujące operacje kryptograficzne.  
- **Front-end (Vue 3 + Vite)** — oferuje nowoczesny interfejs do obsługi plików, generator haseł, pasek siły, log „konsola” i stylowane kontrolki.

---

## 🧩 Technologie

- **ASP.NET Core 8 (.NET 8)** — logika szyfrowania, REST API  
- **C#** — implementacja AEAD, PBKDF2 (Rfc2898DeriveBytes), limit prób, blokowanie pliku  
- **Vue 3 + Vite** — aplikacja SPA, proxy do API w trybie dev  
- **Axios / Fetch** — komunikacja z API (multipart/form-data)  
- **Swagger** — dokumentacja API w środowisku developerskim

---

## ⚙️ Funkcje

- Szyfrowanie plików przy użyciu **AES-GCM** lub **ChaCha20-Poly1305**  
- Odszyfrowywanie z weryfikacją integralności (tag uwierzytelniający)  
- Ochrona haseł poprzez **PBKDF2 (SHA-256)** z solą i iteracjami  
- Opcjonalny **czas wygaśnięcia pliku** (UTC ticks w nagłówku danych)  
- Kontrola liczby nieudanych prób deszyfrowania i blokada pliku  
- Interfejs z:
  - generatorem losowego hasła  
  - paskiem siły hasła  
  - stylowanymi kontrolkami (select, file picker)  
  - logiem „konsola” w UI

---

## 🔒 Struktura zaszyfrowanego pliku

Parametry domyślne (`apps/server/Config/AppSettings.cs`):
- SALT_SIZE = 16
- IV_SIZE = 12
- TAG_SIZE = 16
- HASH_SIZE = 32
- ITERATION_COUNT = 100000

### Układ bajtów (bez daty wygaśnięcia)

1. **Algorithm** (1 bajt)  
   `0 = AES-GCM`, `1 = ChaCha20-Poly1305`  
2. **Salt** (16 bajtów)  
3. **IV / Nonce** (12 bajtów)  
4. **Cipher + Tag** (N + 16 bajtów)  
5. **PasswordHash** (32 bajty, PBKDF2 SHA-256)

### Układ z czasem wygaśnięcia

Dodawane 8 bajtów (`Int64` ticks UTC, little-endian):  
`Algorithm | Salt | IV | ExpireTicks(8) | Cipher + Tag | PasswordHash`

### Logika

- **Encrypt\*** — konkatenuje wszystkie segmenty; opcjonalnie wstawia 8 bajtów czasu po IV.  
- **Decrypt** — odczytuje strukturę, wykrywa obecność czasu, waliduje integralność i hash.  
- **SplitEncryptedData** — rozdziela Cipher i Tag (ostatnie 16 bajtów = tag).

---

## 🔗 API (REST)

### POST `/api/File/encrypt`
- **File** — plik binarny *(wymagane)*  
- **Password** — min. 8 znaków *(wymagane)*  
- **Algorithm** — `AES-GCM` lub `ChaCha20-Poly1305` *(opcjonalne)*  
- **ExpireTime** — ISO8601 (UTC) *(opcjonalne)*  
- **Response:** `application/octet-stream` → plik `*.enc`

### POST `/api/File/decrypt`
- **File** — plik `*.enc` *(wymagane)*  
- **Password** — hasło *(wymagane)*  
- **Response:** `application/octet-stream` → oryginalny plik  

<br>

**Typowe błędy (400):**
- Brak pliku lub błędne rozszerzenie `.enc`  
- Hasło zbyt krótkie  
- Plik wygasł (`ExpireTime`)  
- Nieprawidłowy format pliku  

---

## 🧠 Uruchomienie (dev)

### Back-end (HTTPS)

```bash
cd apps/server
dotnet restore
dotnet run
```

### Front-end (HTTPS)
```bash
cd apps/client
npm install
npm run dev
```

---

## 🛠️ Konfiguracja

### apps/client/.env
```bash
VITE_API_BASE_URL="http://localhost:5024"
```

### apps/client/vite.config.js
```bash
proxy: { '/api': 'https://localhost:5024' } 
```

---

## 🛡️ Uwagi bezpieczeństwa

- Hasła nie są przechowywane — klucz jest wyprowadzany z hasła i soli za pomocą PBKDF2 (SHA-256).
- Tag AEAD (16 bajtów) gwarantuje integralność danych — każda modyfikacja zostanie wykryta.
- Czas wygaśnięcia (jeśli ustawiony) jest walidowany po stronie serwera przed odszyfrowaniem.

---

## 👨‍💻 Autor

![contributors badge](https://readme-contribs.as93.net/contributors/CzechU99/servicehUB)

---

> © 2025 Cryptex – Wszystkie prawa zastrzeżone