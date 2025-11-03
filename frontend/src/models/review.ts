import User from "./user";

export default interface Review {
    author: User;
    rating: number;
    review: string;
    dateWritten?: Date;
    dateVisited?: Date;
    active?: boolean;
}