//Might add a field for reviews, but honestly
//considering the only real use of this right now is 
//seeing reviews on the park, not worth it

export default interface User {
    id: string;
    displayName: string;
    fullName: string;
    dateOfBirth: Date;
}