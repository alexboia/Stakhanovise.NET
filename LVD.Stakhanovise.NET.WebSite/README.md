# Stakhanovise.NET Website

An Astro + Tailwind + React site in a dark, modernized Soviet-poster aesthetic to present and document [Stakhanovise.NET](https://github.com/alexboia/Stakhanovise.NET).

## Running the site

- `npm install`
- `npm run dev` — start the dev server at http://localhost:4321
- `npm run build` — production build to `dist/`
- `npm run preview` — preview the production build locally

## Content model

- Markdown docs live in `src/content/docs`.
- Presentation briefs live in `src/content/presentations`.
- Collections are defined in `src/content/config.ts`.
- Pages auto-route to `/docs/{slug}` and `/presentations/{slug}` with shared layouts.

## Theming

- Tailwind v4 with custom tokens in `src/styles/global.css`.
- Fonts: Russo One (display) + Space Grotesk (body).
- Dark palette with accent red/gold/cyan gradients.

## React usage

- `StatusTicker` (`src/components/StatusTicker.tsx`) is hydrated on the home page to animate slogans.
