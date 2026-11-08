# Uni-Shop

Magazin online cu de toate.

Aplicatie web de tip magazin online, cu catalog de produse, cos de cumparaturi, wish list si panou de administrare. Proiect realizat in scop academic.

## Functionalitati

**Vizitator**

- Rasfoire produse si categorii, cautare si filtrare
- Inregistrare cont

**Client**

- Autentificare si gestionare profil
- Wish list (lista de favorite)
- Cos de cumparaturi
- Finalizare comanda: date de contact, adresa de livrare, modalitate de livrare si plata online
- Istoric comenzi

**Administrator**

- Autentificare si acces in panoul de administrare
- Adaugare, editare si stergere produse si categorii
- Gestionare stocuri, comenzi si utilizatori
- Rapoarte simple de vanzari

## Produsul

Fiecare produs contine: nume, descriere, specificatii, pret, stoc, furnizor, modalitate de livrare si categorie.

## Tehnologii

| Strat          | Tehnologie                            |
| -------------- | ------------------------------------- |
| Frontend       | Angular, TypeScript, Angular Material |
| Backend        | ASP.NET Core (C#, Web API)            |
| Baza de date   | PostgreSQL                            |
| ORM            | Entity Framework Core                 |
| Autentificare  | ASP.NET Core Identity, JWT            |
| Plati          | Stripe (mod test)                     |
| Testare        | xUnit, Postman                        |
| Containerizare | Docker, Docker Compose                |

## Arhitectura

Aplicatia are trei straturi: frontend-ul Angular (prezentare) comunica prin API REST cu serverul ASP.NET Core (logica de business), care acceseaza baza de date PostgreSQL.

## Rulare

```bash
git clone https://github.com/Cirjaliu-Mihai/Uni-Shop.git
cd uni-shop
docker compose up --build
```

Development defaults seed the admin account `admin@unishop.local` with password `Admin1234`. The Development JWT key is local-only; do not use these credentials or that key in production.

Aplicatia va fi disponibila la `http://localhost:4200`, iar API-ul la `http://localhost:5000`.
PostgreSQL este disponibil pe portul `5433`. In Development, documentatia OpenAPI este la `http://localhost:5000/openapi/v1.json`, iar Scalar UI la `http://localhost:5000/scalar`.

Sample product and category photos use direct Unsplash image URLs, so no image files are stored in the repository or API container. They require an internet connection and are subject to Unsplash availability and licensing terms.

Representative sample photos: [laptop](https://images.unsplash.com/photo-1496181133206-80ce9b88a853), [phone](https://images.unsplash.com/photo-1511707171634-5f897ff02aa9), and [headphones](https://images.unsplash.com/photo-1505740420928-5e560c06d30e), from [Unsplash](https://unsplash.com/license).

## Backend si date initiale

In dezvoltare, API-ul aplica migratiile si insereaza datele initiale la pornire. Pentru a inlocui credentialele locale implicite, configureaza user-secrets din directorul `backend/UniShop.Api`:

```bash
dotnet user-secrets set "Admin:Email" "admin@unishop.local"
dotnet user-secrets set "Admin:Password" "LocalPass123"
dotnet user-secrets set "JwtSettings:Key" "replace-with-a-random-secret-of-at-least-32-bytes"
```

Pentru baza de date pornita separat, ruleaza `dotnet ef database update`, apoi `dotnet run` din directorul `backend/UniShop.Api`.
Endpoint-urile de autentificare sunt `POST /api/auth/register`, `POST /api/auth/login` si `GET /api/auth/me`.

## Administrare

Autentifica-te cu `admin@unishop.local` / `Admin1234`, apoi deschide `http://localhost:4200/admin`. Poti gestiona produsele, categoriile, furnizorii si metodele de livrare. API-ul de administrare foloseste rolul `Admin`; incercarile de stergere a unei categorii sau a unui furnizor folosit de produse raspund cu `409 Conflict` si un mesaj explicativ.

## Postman

Importa `docs/postman/UniShop.postman_collection.json`. Colectia foloseste adresa implicita `http://localhost:5000`, autentifica-te prin folderul `Auth` (scriptul de login pastreaza tokenul in variabila colectiei), apoi ruleaza folderele `Products` si `Admin`. Valorile de referinta implicite sunt ID-urile seed-ului Development; actualizeaza variabilele `categoryId`, `supplierId` sau `deliveryMethodId` daca baza de date a fost modificata.

Ruleaza testele API cu PostgreSQL in Docker si testele Angular astfel:

```bash
dotnet test
npm --prefix frontend test -- --watch=false
```

## Structura proiectului

```
/backend    # ASP.NET Core Web API
/frontend   # aplicatia Angular
/docs       # rapoarte si diagrame
```

## Autor

Cirjaliu Mihai
