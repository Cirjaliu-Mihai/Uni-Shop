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

Aplicatia va fi disponibila la `http://localhost:4200`, iar API-ul la `http://localhost:5000`.

## Structura proiectului

```
/backend    # ASP.NET Core Web API
/frontend   # aplicatia Angular
/docs       # rapoarte si diagrame
```

## Autor

Cirjaliu Mihai
