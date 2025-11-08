# Dirt Bike Park App Demo Run Book

This run book covers how to demo the five key API endpoints using Swagger UI or PowerShell/curl. All commands assume the API is running on http://localhost:5200.

## 1. Start the API

Open PowerShell and run:
```powershell
$env:ASPNETCORE_URLS = "http://localhost:5200"; dotnet run --project "c:\VS Projects\CSCE547\Dirt Bike Park App\DirtBikePark.Cli"
```

Access Swagger UI at:
- http://localhost:5200/swagger

---

## 2. Endpoints to Demo


### A. AddPark
- **Endpoint:** POST /api/parks
- **Swagger:** Use "POST /api/parks" and fill in the request body.

- **PowerShell Example:**
```powershell
$body = @{
    name = "Foothills Trail Center"
    description = "Flowy single track with beginner clinics"
    location = "Asheville, NC"
    guestLimit = 20
    pricePerGuestPerDay = 95
    currency = "USD"
    availableDates = @( (Get-Date).AddDays(1).ToString('yyyy-MM-dd') )
} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5200/api/parks" -Method Post -Body $body -ContentType "application/json"
```

- **curl Example:**
```bash
curl -X POST http://localhost:5200/api/parks \
   -H "Content-Type: application/json" \
   -d '{
      "name": "Foothills Trail Center",
      "description": "Flowy single track with beginner clinics",
      "location": "Asheville, NC",
      "guestLimit": 20,
      "pricePerGuestPerDay": 95,
      "currency": "USD",
      "availableDates": ["2025-11-09"]
   }'
```


### B. CreateBooking
- **Endpoint:** POST /api/parks/{parkId}/bookings
- **Swagger:** Use "POST /api/parks/{parkId}/bookings" with a valid parkId.

- **PowerShell Example:**
```powershell
# Get a parkId
$parks = Invoke-RestMethod -Uri "http://localhost:5200/api/parks"
$parkId = $parks[0].id

# Create a booking
$bookingBody = @{
    guestName = "Jane Smith"
    guests = 3
    dayCount = 2
    guestCategoryName = "Adult"
    date = (Get-Date).AddDays(2).ToString('yyyy-MM-dd')
} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5200/api/parks/$parkId/bookings" -Method Post -Body $bookingBody -ContentType "application/json"
```

- **curl Example:**
```bash
curl -X POST http://localhost:5200/api/parks/{parkId}/bookings \
   -H "Content-Type: application/json" \
   -d '{
      "guestName": "Jane Smith",
      "guests": 3,
      "dayCount": 2,
      "guestCategoryName": "Adult",
      "date": "2025-11-10"
   }'
# Replace {parkId} with a valid park ID from GET /api/parks
```


### C. AddBookingToCart
- **Endpoint:** POST /api/carts/{cartId}/items
- **Swagger:** Use "POST /api/carts/{cartId}/items" with bookingId and quantity.

- **PowerShell Example:**
```powershell
# Create or retrieve a cart
$cart = Invoke-RestMethod -Uri "http://localhost:5200/api/carts"

# Add booking to cart
$addBody = @{
    bookingId = $booking.id
    quantity = 1
} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5200/api/carts/$($cart.cartId)/items" -Method Post -Body $addBody -ContentType "application/json"
```

- **curl Example:**
```bash
curl -X POST http://localhost:5200/api/carts/{cartId}/items \
   -H "Content-Type: application/json" \
   -d '{
      "bookingId": "{bookingId}",
      "quantity": 1
   }'
# Replace {cartId} and {bookingId} with actual values
```


### D. GetCart
- **Endpoint:** GET /api/carts?id={cartId}
- **Swagger:** Use "GET /api/carts" and provide cartId as query param.

- **PowerShell Example:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5200/api/carts?id=$($cart.cartId)"
```

- **curl Example:**
```bash
curl http://localhost:5200/api/carts?id={cartId}
# Replace {cartId} with your cart ID
```


### E. GetParks
- **Endpoint:** GET /api/parks
- **Swagger:** Use "GET /api/parks".

- **PowerShell Example:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5200/api/parks"
```

- **curl Example:**
```bash
curl http://localhost:5200/api/parks
```

---

## 3. Demo Flow
1. Start the API and open Swagger UI.
2. Use Swagger or PowerShell to:
   - Add a new park.
   - List parks and get a parkId.
   - Create a booking for a park.
   - Create/retrieve a cart and add the booking to the cart.
   - Retrieve the cart and show its contents.
   - List all parks.

## 4. Notes
- All endpoints accept and return JSON.
- Dates must be in yyyy-MM-dd format.
- You can use curl instead of PowerShell if preferred.
- Swagger UI provides example requests and responses for each endpoint.

---

Prepared for demo on November 8, 2025.
