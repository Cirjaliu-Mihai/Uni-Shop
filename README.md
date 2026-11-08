# UniShop

UniShop is an online-store application.

## Features

- **Visitors:** browse and filter products, view categories, and register.
- **Clients:** manage profile and addresses, use a cart and wishlist, place orders, and view order history.
- **Admins:** manage products and reference data, fulfill orders, manage users, and view sales reports.

## Stack

Angular and Angular Material; ASP.NET Core Web API; PostgreSQL and Entity Framework Core; ASP.NET Identity and JWT; Docker Compose; Stripe Checkout in test mode.

## Run

Requirements: Git and Docker Desktop with Compose.

```powershell
git clone https://github.com/Cirjaliu-Mihai/Uni-Shop.git
cd Uni-Shop
docker compose up --build
```

Open the store at http://localhost:4200. The API explorer is at http://localhost:5000/scalar. PostgreSQL is available on port `5433`.

The seeded demo admin is `admin@unishop.local` / `Admin1234`. Register a client at http://localhost:4200/account/register.

## Stripe test payments

Stripe is optional; cash on delivery works without it. To test card checkout, put your Stripe test keys in the repository-root `.env` file. Use `.env.example` as a template; `.env` is ignored by Git.

For local webhooks, run these commands in a separate terminal and copy the printed `whsec_...` value into `.env` as `STRIPE_WEBHOOK_SECRET`:

```powershell
npx @stripe/cli login
npx @stripe/cli listen --events checkout.session.completed --forward-to localhost:5000/api/payments/webhook
```

Keep the listener running and recreate the API after changing `.env`:

```powershell
docker compose up -d --force-recreate api
```

Use test card `4242 4242 4242 4242`, any future expiry date, and any CVC.

## Troubleshooting

- Start Docker Desktop before running Compose.
- PostgreSQL is exposed on port `5433`.
- Reset the database with `docker compose down -v` (this deletes its data).
