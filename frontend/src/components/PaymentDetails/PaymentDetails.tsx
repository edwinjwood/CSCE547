import { useState } from "react";

export default function PaymentDetails() {

    const [cardNumber, setCardNumber] = useState("");
    const [expDate, setExpDate] = useState("");
    const [name, setName] = useState("");

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
            cvc: "",
            street: "",
            city: "",
            state: "",
            postalCode: "",
        };

        await fetch("http://localhost:5000/api/checkout", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(paymentData),
        });

        setCardNumber("");
        setExpDate("");
        setName("");
    }

    return (
        <div>
            <div>
                <label>Card Number</label>
                <input type="text" onChange={e => setCardNumber(e.target.value.replace(/\D/, ''))} value={cardNumber} />
            </div>
            <div>
                <label>Expiration Date</label>
                <input type="text" onChange={handleExpDate} value={expDate} />
            </div>
            <div>
                <label>Name on Card</label>
                <input type="text" onChange={handleNameChange} value={name}/>
            </div>
            <button onClick={() => sendCardDetails()}>Submit Payment</button>
        </div>
    )
}