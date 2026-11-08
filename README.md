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

## Stripe test payments

Card payments use Stripe-hosted Checkout; card details never pass through the UniShop API. Configure test keys outside committed files. For local `dotnet run`, use user-secrets from `backend/UniShop.Api`:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_replace_with_your_key"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_replace_with_cli_secret"
```

For Docker Compose, copy `.env.example` to `.env` in the repository root and fill in the same test keys as `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET`. `.env` is ignored by Git; never commit real Stripe keys.

The Stripe CLI listener must stay running during local tests. Install/run it through `npx` if you do not want a global CLI installation. Log in, then forward only the Checkout event this API handles. Use the `whsec_...` value it prints as `Stripe:WebhookSecret` (or `STRIPE_WEBHOOK_SECRET` in `.env`):

```powershell
npx @stripe/cli login
npx @stripe/cli listen --events checkout.session.completed --forward-to localhost:5000/api/payments/webhook
```

Card orders stay Pending until the verified `checkout.session.completed` webhook arrives. A worker cancels unpaid card orders after 30 minutes and restores inventory. For a successful test payment, use `4242 4242 4242 4242`, any future expiry date, and any CVC.

If Stripe keys are not configured, card-session creation returns `503 Service Unavailable`; cash-on-delivery checkout remains available.

## Admin users and reports

The admin panel includes `/admin/users` for searching and paging through accounts, viewing roles/order counts/lock state, and locking, unlocking, or changing roles. The API prevents self-locking/self-demotion and protects the last administrator. `/admin/reports` shows daily, weekly, or monthly sales and the top-selling products for a selected date range. Cancelled orders are excluded from both reports.

The corresponding APIs are `GET /api/admin/users`, `PATCH /api/admin/users/{id}/lock`, `PATCH /api/admin/users/{id}/unlock`, `PATCH /api/admin/users/{id}/role`, `GET /api/admin/reports/sales`, and `GET /api/admin/reports/popular-products`.

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
