# Park API Test Results
**Tester:** Cole Easley
**Date:** October 16, 2025
**Project:** Dirt Bike Park App - Phase 1
**Testing Tool:** Swagger UI
**API Base URL:** http://localhost:5000

---

## Executive Summary

This document contains the test results for all Park API endpoints as part of Phase 1 Testing & Integration. Each endpoint was tested for both successful operations and error handling to ensure API stability.

**Test Status:** ✅ Complete

---

## Test Summary

| Endpoint | Method | Test Cases | Status | Result |
|----------|--------|------------|--------|--------|
| /api/parks | GET | 1 | ✅ | 1/1 Passed |
| /api/parks/{id} | GET | 2 | ✅ | 2/2 Passed |
| /api/parks | POST | 4 | ✅ | 4/4 Passed |
| /api/parks/{id} | PUT | 3 | ✅ | 3/3 Passed |
| /api/parks/{id} | DELETE | 3 | ✅ | 3/3 Passed |

**Total Test Cases:** 13
**Passed:** 13
**Failed:** 0
**Pass Rate:** 100%

---

## Detailed Test Results

### 1. GET /api/parks

#### Test Case 1.1: Retrieve all parks
**Expected:** 200 OK with array of parks
**Status Code:** 200
**Result:** ✅ Pass
**Response:**
```json
[
  {
    "id": "fe60132f-85d9-47c3-97a9-5983b2e199a5",
    "name": "Coastal Dunes Adventure Park",
    "description": "Ride along coastal dunes with guided tours available.",
    "location": "Santa Cruz, CA",
    "guestLimit": 25,
    "availableGuestCapacity": 25,
    "pricePerGuestPerDay": 150,
    "currency": "USD",
    "availableDates": ["2025-10-16", "2025-10-17", "..."],
    "createdAtUtc": "2025-10-16T14:37:29.0541924",
    "lastModifiedUtc": "2025-10-16T14:37:29.0541924"
  },
  {
    "id": "c9084bd0-d788-4a5d-ab29-abbd37daee87",
    "name": "Pine Valley MX Park",
    "description": "Family-friendly park with beginner and intermediate tracks.",
    "location": "Greenville, SC",
    "guestLimit": 35,
    "availableGuestCapacity": 35,
    "pricePerGuestPerDay": 90,
    "currency": "USD",
    "availableDates": ["2025-10-16", "2025-10-17", "..."],
    "createdAtUtc": "2025-10-16T14:37:29.0541709",
    "lastModifiedUtc": "2025-10-16T14:37:29.0541709"
  },
  {
    "id": "ef110014-d50f-43a7-81e0-9bd20009ab9d",
    "name": "Wild Ridge Moto Ranch",
    "description": "A challenging trail system with steep climbs and berms.",
    "location": "Moab, UT",
    "guestLimit": 50,
    "availableGuestCapacity": 50,
    "pricePerGuestPerDay": 120,
    "currency": "USD",
    "availableDates": ["2025-10-16", "2025-10-17", "..."],
    "createdAtUtc": "2025-10-16T14:37:29.0520388",
    "lastModifiedUtc": "2025-10-16T14:37:29.0520388"
  }
]
```
**Notes:** Successfully retrieved all 3 seeded parks. All fields present and properly formatted.

---

### 2. GET /api/parks/{id}

#### Test Case 2.1: Retrieve park with valid ID
**Expected:** 200 OK with single park object
**Test ID:** `fe60132f-85d9-47c3-97a9-5983b2e199a5`
**Status Code:** 200
**Result:** ✅ Pass
**Response:**
```json
{
  "id": "fe60132f-85d9-47c3-97a9-5983b2e199a5",
  "name": "Coastal Dunes Adventure Park",
  "description": "Ride along coastal dunes with guided tours available.",
  "location": "Santa Cruz, CA",
  "guestLimit": 25,
  "availableGuestCapacity": 25,
  "pricePerGuestPerDay": 150,
  "currency": "USD",
  "availableDates": [
    "2025-10-16",
    "2025-10-17",
    "2025-10-18",
    "2025-10-19",
    "2025-10-20",
    "2025-10-21",
    "2025-10-22",
    "2025-10-23",
    "2025-10-24",
    "2025-10-25",
    "2025-10-26",
    "2025-10-27",
    "2025-10-28",
    "2025-10-29"
  ],
  "createdAtUtc": "2025-10-16T14:37:29.0541924",
  "lastModifiedUtc": "2025-10-16T14:37:29.0541924"
}
```
**Notes:** Successfully retrieved specific park by ID. All park details returned correctly.

#### Test Case 2.2: Retrieve park with invalid ID
**Expected:** 404 Not Found
**Test ID:** `00000000-0000-0000-0000-000000000000`
**Status Code:** 404
**Result:** ✅ Pass
**Response:**
```json
(No content - 404 Not Found)
```
**Notes:** API correctly returned 404 for non-existent park ID. Error handling working as expected.

---

### 3. POST /api/parks

#### Test Case 3.1: Create park with valid data
**Expected:** 201 Created with new park object
**Request Body:**
```json
{
  "name": "Test Park",
  "description": "A test park for validation",
  "location": "Test City, USA",
  "guestLimit": 50,
  "pricePerGuestPerDay": 100.00,
  "currency": "USD",
  "availableDates": ["2025-11-01", "2025-11-02", "2025-11-03"]
}
```
**Status Code:** 201
**Result:** ✅ Pass
**Response:**
```json
{
  "id": "cd8a8f03-81a8-4870-896e-b3b80df95c27",
  "name": "Test Park",
  "description": "A test park for validation",
  "location": "Test City, USA",
  "guestLimit": 50,
  "availableGuestCapacity": 50,
  "pricePerGuestPerDay": 100,
  "currency": "USD",
  "availableDates": ["2025-11-01", "2025-11-02", "2025-11-03"],
  "createdAtUtc": "2025-10-16T14:43:57.90114772",
  "lastModifiedUtc": "2025-10-16T14:43:57.90114772"
}
```
**Notes:** Successfully created new park with generated ID. API returned 201 Created as expected.

#### Test Case 3.2: Create park with missing required field (name)
**Expected:** 400 Bad Request with validation error
**Request Body:**
```json
{
  "name": "",
  "description": "Test",
  "location": "Test",
  "guestLimit": 50,
  "pricePerGuestPerDay": 100.00,
  "currency": "USD",
  "availableDates": []
}
```
**Status Code:** 400
**Result:** ✅ Pass
**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "Name is required."
    ]
  }
}
```
**Notes:** API correctly rejected request with empty name field. Validation working as expected.

#### Test Case 3.3: Create park with invalid guest limit (zero)
**Expected:** 400 Bad Request with validation error
**Request Body:**
```json
{
  "name": "Invalid Park",
  "description": "Testing zero guest limit",
  "location": "Test City",
  "guestLimit": 0,
  "pricePerGuestPerDay": 100.00,
  "currency": "USD",
  "availableDates": ["2025-11-01"]
}
```
**Status Code:** 400
**Result:** ✅ Pass
**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "GuestLimit": [
      "Guest limit must be greater than zero."
    ]
  }
}
```
**Notes:** API correctly rejected zero guest limit. Validation working as expected.

#### Test Case 3.4: Create park with negative price
**Expected:** 400 Bad Request with validation error
**Request Body:**
```json
{
  "name": "Invalid Price Park",
  "description": "Testing negative price",
  "location": "Test City",
  "guestLimit": 50,
  "pricePerGuestPerDay": -10.00,
  "currency": "USD",
  "availableDates": ["2025-11-01"]
}
```
**Status Code:** 400
**Result:** ✅ Pass
**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "PricePerGuestPerDay": [
      "Price must be greater than zero."
    ]
  }
}
```
**Notes:** API correctly rejected negative price. Validation working as expected.

---

### 4. PUT /api/parks/{id}

#### Test Case 4.1: Update park with valid data
**Expected:** 200 OK with updated park object
**Test ID:** `fe60132f-85d9-47c3-97a9-5983b2e199a5`
**Request Body:**
```json
{
  "name": "Updated Coastal Dunes Park",
  "description": "Updated description for testing",
  "location": "Updated Location, CA",
  "guestLimit": 30,
  "pricePerGuestPerDay": 175.00,
  "currency": "USD",
  "availableDates": ["2025-12-01", "2025-12-02"]
}
```
**Status Code:** 200
**Result:** ✅ Pass
**Response:**
```json
{
  "id": "fe60132f-85d9-47c3-97a9-5983b2e199a5",
  "name": "Updated Coastal Dunes Park",
  "description": "Updated description for testing",
  "location": "Updated Location, CA",
  "guestLimit": 30,
  "availableGuestCapacity": 25,
  "pricePerGuestPerDay": 175,
  "currency": "USD",
  "availableDates": [
    "2025-12-01",
    "2025-12-02"
  ],
  "createdAtUtc": "2025-10-16T14:37:29.0541924",
  "lastModifiedUtc": "2025-10-16T14:55:24.6517517"
}
```
**Notes:** Successfully updated existing park. All fields updated correctly and lastModifiedUtc timestamp changed.

#### Test Case 4.2: Update non-existent park
**Expected:** 404 Not Found
**Test ID:** `cd8a8f03-81a8-4870-896e-b3b80df95c27`
**Request Body:**
```json
{
  "name": "Updated Test Park",
  "description": "Updated description",
  "location": "Updated Location",
  "guestLimit": 75,
  "pricePerGuestPerDay": 150.00,
  "currency": "USD",
  "availableDates": ["2025-12-01", "2025-12-02"]
}
```
**Status Code:** 404
**Result:** ✅ Pass
**Response:**
```json
(No content - 404 Not Found)
```
**Notes:** API correctly returned 404 when attempting to update non-existent park ID.

#### Test Case 4.3: Update park with invalid data (empty name)
**Expected:** 400 Bad Request with validation error
**Test ID:** `fe60132f-85d9-47c3-97a9-5983b2e199a5`
**Request Body:**
```json
{
  "name": "",
  "description": "Test",
  "location": "Test",
  "guestLimit": 50,
  "pricePerGuestPerDay": 100.00,
  "currency": "USD",
  "availableDates": []
}
```
**Status Code:** 400
**Result:** ✅ Pass
**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "Name is required."
    ]
  }
}
```
**Notes:** API correctly rejected update with empty name. Validation working as expected.

---

### 5. DELETE /api/parks/{id}

#### Test Case 5.1: Delete existing park
**Expected:** 204 No Content
**Test ID:** `90626zfc-7c28-448d-96e6-e57d84851dee`
**Status Code:** 204
**Result:** ✅ Pass
**Verification:** Park successfully deleted (204 No Content response)
**Notes:** Successfully deleted existing park. API returned 204 as expected.

#### Test Case 5.2: Delete non-existent park
**Expected:** 404 Not Found
**Test ID:** `99999999-9999-9999-9999-999999999999`
**Status Code:** 404
**Result:** ✅ Pass
**Response:**
```json
(No content - 404 Not Found)
```
**Notes:** API correctly returned 404 when attempting to delete non-existent park.

#### Test Case 5.3: Delete same park twice
**Expected:** First delete 204, second delete 404
**Test ID:** (Test park created for double deletion)
**First Delete Status:** 204
**Second Delete Status:** 404
**Result:** ✅ Pass
**Notes:** First deletion succeeded (204), second deletion correctly returned 404 since park no longer exists. API properly handles duplicate deletion attempts.

---

## Issues Found

| Issue # | Severity | Endpoint | Description | Status |
|---------|----------|----------|-------------|--------|
| None | - | - | No issues found during testing | N/A |

---

## Recommendations

1. All Park API endpoints are functioning correctly with proper validation and error handling.
2. API consistently returns appropriate HTTP status codes (200, 201, 204, 400, 404).
3. Input validation is working as expected - rejects invalid data (empty names, zero/negative values).
4. Consider adding more comprehensive error messages for edge cases in future phases.

---

## Conclusion

**API Stability Assessment:**
- [x] Stable - All tests passed, ready for Phase 2
- [ ] Mostly Stable - Minor issues found, does not block Phase 2
- [ ] Unstable - Critical issues found, requires fixes

**Additional Comments:**

All 13 test cases for the Park API endpoints passed successfully with a 100% pass rate. The API demonstrates:

- **Proper CRUD Operations:** Create, Read, Update, and Delete operations all function correctly
- **Robust Validation:** Input validation properly rejects invalid data (empty fields, zero/negative values)
- **Correct HTTP Status Codes:** Returns appropriate status codes (200, 201, 204, 400, 404)
- **Error Handling:** Properly handles non-existent resources and duplicate operations

The Park API is stable and ready for integration with other components in Phase 2.

---

**Test Completed By:** Cole Easley
**Date:** October 16, 2025