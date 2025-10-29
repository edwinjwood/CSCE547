import IPark from "./park";

export interface CartItem {
    park: IPark;
    numDays: number;
    numAdults: number;
    numKids: number;
}