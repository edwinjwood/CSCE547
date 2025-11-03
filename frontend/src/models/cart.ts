import { CartItem } from "./cartItem";

export interface Cart {
    cartItems: CartItem[];
    taxRate: number;
    total: number;
}