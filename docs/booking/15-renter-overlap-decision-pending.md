# Booking decision pending — Renter multi-asset overlap

**Status:** OPEN — business decision required (not a bug)  
**Date:** 2026-07-20  

## Context

Availability today enforces:

> Blocking bookings on the **same asset** must not overlap (including preparation buffer).

There is **no** invariant that prevents the same renter from holding overlapping bookings on **different** assets.

## Question for product / architect

May one renter reserve multiple assets for overlapping calendar periods?

Examples where **yes** is valid:

- Two cars for a family trip  
- Multiple equipment units for a company job  
- Host-as-renter edge cases (if allowed)

## If decision = No (block renter overlap)

Implement as a separate change set:

1. Application validation query (renter + date range + blocking statuses)  
2. Optional DB constraint / transactional lock  
3. Integration tests for conflict + allow adjacent days  

## If decision = Yes (allow)

Document as explicit BR and close this item. No code change.

## Recommendation

Do **not** implement renter-overlap blocking until product answers the question above.
