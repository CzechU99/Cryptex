Struktura pliku: salt (16) | iv (12) | cipher + tag | passwordHash (32)<h2 align="center"><strong>Cryptex – System szyfrowania i weryfikacji integralności plików</strong></h2>

<div align="center">
    <p>
      <img alt="Status" src="https://img.shields.io/badge/status-in develope-blue">
      <img alt="Licencja" src="https://img.shields.io/badge/licencja-private-lightgrey">
    </p>
    <p>
      <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white">
      <img alt="ASP.NET Core" src="https://img.shields.io/badge/ASP.NET_Core-512BD4?logo=dotnet&logoColor=white">
      <img alt="C#" src="https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white">
    </p>
</div>

---

## 🎯 Cel projektu

**Cryptex** to aplikacja webowa służąca do **bezpiecznego szyfrowania, odszyfrowywania i weryfikacji integralności plików**.  
Projekt wykorzystuje nowoczesne algorytmy kryptograficzne (AES-GCM, ChaCha20-Poly1305), zapewnia ochronę przed modyfikacją danych oraz kontroluje liczbę nieudanych prób deszyfrowania. System został zaprojektowany w architekturze **ASP.NET Core** z wykorzystaniem **C#**.

---

## 🧱 Technologie

- **ASP.NET Core 8.0** – warstwa serwerowa (REST API)  
- **C#** – logika biznesowa i obsługa kryptografii  
- **AesGcm / ChaCha20Poly1305** – nowoczesne algorytmy szyfrowania  
- **Swagger (OpenAPI)** – dokumentacja endpointów  

---

## 🔐 Funkcjonalności aplikacji

| Funkcja | Opis |
|----------|------|
| 🔑 Szyfrowanie plików | Użycie silnych algorytmów AES-GCM lub ChaCha20-Poly1305. |
| 🧾 Weryfikacja integralności | Sprawdzenie, czy plik nie został zmodyfikowany po zaszyfrowaniu. |
| 🧩 Haszowanie hasła | Bezpieczne haszowanie hasła użytkownika z użyciem Rfc2898DeriveBytes. |
| 🕵️‍♂️ Kontrola prób deszyfrowania | Licznik nieudanych prób, blokada użytkownika po przekroczeniu limitu. |
| 🧠 Obsługa wyjątków | Różne komunikaty dla błędnego hasła i naruszenia integralności. |
| 🌐 REST API | Szyfrowanie i deszyfrowanie dostępne przez endpointy API. |
| 🧱 Czas ważności pliku | Ustawienie do kiedy plik może zostać odszyfrowany. |

---


