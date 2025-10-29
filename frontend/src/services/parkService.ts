import IPark from "../models/park";

export default class ParkService {

    public parks: IPark[] = [];

    getAllParks: () => Promise<IPark[]> = async () => {
        const parks = mockData.map((val) => JSON.parse(JSON.stringify(val)))
        return new Promise((res) => {
            setTimeout(() => {
                res(parks)
            }, 300);
        });
    };

    getParkById: (id: string) => Promise<IPark> = async (id: string) => {
        const parks = mockData.map((val) => JSON.parse(JSON.stringify(val)));
        return new Promise((res) => {
            setTimeout(() => {
                res(parks.find((park) => park.id === id))
            }, 500)
        })
    }
}

const mockData = [
    {
        "parkName": "Motobike Mayhem",
        "id": "92ed4740-12d9-4573-a8f1-c883ca216a00",
        "location": "Springwood, CO",
        "description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
        "adultPrice": 25,
        "childPrice": 15,
        "imageUrl": "https://placehold.co/600x400/334155/FFF?text=Motobike+Mayhem",
        "reviews": [
            {
                "author": {
                    "id": "17fa0861-d120-4cd8-a24b-f9a579ecbf17",
                    "displayName": "Moto R. Bike",
                    "fullName": "James Sheldon",
                    "dateOfBirth": "1987-04-23T18:25:43.511Z"
                },
                "rating": 5,
                "dateWritten": "2025-09-15T12:30:32.594Z",
                "dateVisited": "2025-09-13T00:00:00.000Z",
                "review": "Phenomenal park!"
            },
            {
                "author": {
                    "id": "bb7e4654-505c-483a-ae1d-ffcbd1a44d8b",
                    "displayName": "Riciela Rider",
                    "fullName": "Tina Stars",
                    "dateOfBirth": "1956-02-23T07:16:43.511Z"
                },
                "rating": 4,
                "dateWritten": "2025-09-12T12:30:32.594Z",
                "dateVisited": "2025-09-11T00:00:00.000Z",
                "review": "Great time but missed the mark on the rides for me"
            },
            {
                "author": {
                    "id": "704cbc8b-5758-4564-926f-157177428d43",
                    "displayName": "Wrangled Rider",
                    "fullName": "Bill Stars",
                    "dateOfBirth": "1964-12-23T07:16:43.511Z"
                },
                "rating": 5,
                "dateWritten": "2025-09-12T12:30:32.594Z",
                "dateVisited": "2025-09-11T00:00:00.000Z",
                "review": "I had a blast but I think my wife was a little sick from some of the hills :("
            },
            {
                "author": {
                    "id": "bd51ec1d-a13c-4f54-bee8-167181291df5",
                    "displayName": "Removed by Content Moderation Team",
                    "fullName": "Philip Shead",
                    "dateOfBirth": "1998-07-13T12:38:31.029Z"    
                },
                "rating": 1,
                "dateWritten": "2025-07-09T12:30:32.594Z",
                "dateVisited": "2025-06-12T00:00:00.000Z",
                "review": "this plaec SUks, Crossbar Parkway iz wy btr"
            }
        ]
    },
    {
        "parkName": "Crossbar Parkway",
        "id": "fc099512-96d4-497a-a42f-d7b3967abc03",
        "location": "Springwood, CO",
        "description": "This park boasts extreme hills and fun drops for all thrill-seekers.",
        "adultPrice": 25,
        "childPrice": 15,
        "imageUrl": "https://placehold.co/600x400/3321a5/FFF?text=Crossbar+Parkway",
        "reviews": [
            {
                "author": {
                    "id": "17fa0861-d120-4cd8-a24b-f9a579ecbf17",
                    "displayName": "Moto R. Bike",
                    "fullName": "James Sheldon",
                    "dateOfBirth": "1987-04-23T18:25:43.511Z"
                },
                "rating": 0,
                "dateWritten": "2025-09-12T12:30:32.594Z",
                "dateVisited": "2025-09-11T00:00:00.000Z",
                "review": "Phenomenal park!"
            },
            {
                "author": {
                    "id": "bb7e4654-505c-483a-ae1d-ffcbd1a44d8b",
                    "displayName": "Riciela Rider",
                    "fullName": "Tina Stars",
                    "dateOfBirth": "1956-02-23T07:16:43.511Z"
                },
                "rating": 2,
                "dateWritten": "2025-09-12T12:30:32.594Z",
                "dateVisited": "2025-09-11T00:00:00.000Z",
                "review": "Great time but missed the mark on the rides for me"
            },
            {
                "author": {
                    "id": "704cbc8b-5758-4564-926f-157177428d43",
                    "displayName": "Wrangled Rider",
                    "fullName": "Bill Stars",
                    "dateOfBirth": "1964-12-23T07:16:43.511Z"
                },
                "rating": 1,
                "dateWritten": "2025-09-12T12:30:32.594Z",
                "dateVisited": "2025-09-11T00:00:00.000Z",
                "review": "I had a blast but I think my wife was a little sick from some of the hills :("
            },
            {
                "author": {
                    "id": "bd51ec1d-a13c-4f54-bee8-167181291df5",
                    "displayName": "Removed by Content Moderation Team",
                    "fullName": "Philip Shead",
                    "dateOfBirth": "1998-07-13T12:38:31.029Z"    
                },
                "rating": 5,
                "dateWritten": "2025-09-12T12:30:32.594Z",
                "dateVisited": "2025-09-11T00:00:00.000Z",
                "review": "this plaec SUks, Crossbar Parkway iz wy btr"
            }
        ]
    }
] 