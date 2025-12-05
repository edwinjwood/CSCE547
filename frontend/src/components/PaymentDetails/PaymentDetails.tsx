import { useState } from "react";

export default function PaymentDetails() {

    const [cardNumber, setCardNumber] = useState("");
    const [expDate, setExpDate] = useState("");
    const [name, setName] = useState("");
    const [cvc, setCvc] = useState("");
    const [street, setStreet] = useState("");
    const [city, setCity] = useState("");
    const [state, setState] = useState("");
    const [postalCode, setPostalCode] = useState("");

    const handleExpDate = (e: React.ChangeEvent<HTMLInputElement>) => {
        setExpDate(e.target.value)
    }

    const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setName(e.target.value);
    }

    const sendCardDetails = async () => {
        const cartId = localStorage.getItem("cartId");

        if (!cartId) {
            alert("No cart found. Please add items to your cart first.");
            return;
        }

        const paymentData = {
            cartId,
            cardholderName: name,
            cardNumber,
            expirationMonthYear: expDate,
            cvc,
            street,
            city,
            state,
            postalCode,
        };

        try {
            const response = await fetch("http://localhost:5000/api/checkout", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(paymentData),
            });

            const result = await response.json();

            if (response.ok && result.success) {
                alert("Payment successful! Your booking has been confirmed.");
                // Clear form
                setCardNumber("");
                setExpDate("");
                setName("");
                setCvc("");
                setStreet("");
                setCity("");
                setState("");
                setPostalCode("");
                // Clear cart
                localStorage.removeItem("cartId");
            } else {
                alert(`Payment failed: ${result.message || "Unknown error"}`);
            }
        } catch (error) {
            console.error("Payment error:", error);
            alert("Payment failed. Please check your connection and try again.");
        }
    }

    return (
        <div style={{ maxWidth: '500px', margin: '20px auto', padding: '20px' }}>
            <h2>Payment Information</h2>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>Card Number</label>
                <input
                    type="text"
                    onChange={e => setCardNumber(e.target.value.replace(/\D/, ''))}
                    value={cardNumber}
                    placeholder="1234567890123456"
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>Expiration Date (MM/YY)</label>
                <input
                    type="text"
                    onChange={handleExpDate}
                    value={expDate}
                    placeholder="12/25"
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>CVC</label>
                <input
                    type="text"
                    onChange={e => setCvc(e.target.value.replace(/\D/, ''))}
                    value={cvc}
                    placeholder="123"
                    maxLength={4}
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>Name on Card</label>
                <input
                    type="text"
                    onChange={handleNameChange}
                    value={name}
                    placeholder="John Doe"
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <h3 style={{ marginTop: '30px' }}>Billing Address</h3>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>Street Address</label>
                <input
                    type="text"
                    onChange={e => setStreet(e.target.value)}
                    value={street}
                    placeholder="123 Main St"
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>City</label>
                <input
                    type="text"
                    onChange={e => setCity(e.target.value)}
                    value={city}
                    placeholder="Springfield"
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>State</label>
                <input
                    type="text"
                    onChange={e => setState(e.target.value)}
                    value={state}
                    placeholder="CA"
                    maxLength={2}
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <div style={{ marginBottom: '15px' }}>
                <label style={{ display: 'block', marginBottom: '5px' }}>Postal Code</label>
                <input
                    type="text"
                    onChange={e => setPostalCode(e.target.value)}
                    value={postalCode}
                    placeholder="12345"
                    style={{ width: '100%', padding: '8px' }}
                    required
                />
            </div>

            <button
                onClick={() => sendCardDetails()}
                style={{
                    width: '100%',
                    padding: '12px',
                    backgroundColor: '#007bff',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '16px',
                    marginTop: '20px'
                }}
            >
                Submit Payment
            </button>
        </div>
    )
}