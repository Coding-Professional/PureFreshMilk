# 0002. Store and transmit milk quantities as whole millilitres

- **Status:** Proposed
- **Date:** 2026-10-29
- **Deciders:** Project maintainer (pending review)

## Context

Deliveries are measured in litres and millilitres (1 L, 500 ml, 250 ml). Monthly totals and bills are
sums of many daily values. Binary floating point cannot represent values like 0.1 L exactly, so
sums can drift and different layers could disagree about a total.

## Decision

- Quantities are integers in **millilitres** in every layer: the Domain `Quantity` value object,
  database columns, API DTOs (`…Ml` fields) and the web app.
- `Quantity` rejects negative values and fractions of a millilitre, and adds with overflow checking.
- Litres are only a display format (`formatQuantity` in the web app).

## Consequences

- Totals are exact and identical on the server and in any client.
- Field names must make the unit explicit (`expectedMl`, `deliveredMl`).
- Money will need its own decision (for example decimal amounts or integer minor units with
  an explicit currency) when billing is designed. This ADR does not cover money.

## Alternatives considered

- **Decimal litres:** exact, but every boundary (JSON, JavaScript numbers) needs care to stay decimal.
- **Floating-point litres:** simplest to write, but totals can be wrong.
