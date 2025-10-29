import { useEffect, useState } from "react";
import FeaturedParks from "../../components/FeaturedParks/featuredParks";
import HeroContent from "../../components/HeroContent/heroContent";
import Search from "../../components/Search/search";
import CartService from "../../services/cartService";
import ParkService from "../../services/parkService";
import './home.css';
import IPark from "../../models/park";
import Review from "../../models/review";

interface HomeProps {
    parkService: ParkService
    cartService: CartService;
}

type searchParams = {
    location: string,
    rating: number
}

export default function Home(props: HomeProps) {
    const { parkService } = props;

    const [parks, setParks] = useState([] as IPark[])
    //For the purposes of this, empty string location and rating 0 will be considered default
    const [searchParams, setSearchParams] = useState({location: "", rating: 0} as searchParams)

    useEffect(() => {
        parkService.getAllParks().then((res) => {
            setParks(res);
        })
    }, [parkService])

    //This should be done with actual geolocation but none of these parks are real
    //So to simulate, it's just going to be a random chance
    const isNear = (firstLocation: string, secondLocation: string) => {
        const randomChance = Math.random()
        return randomChance > .2 ? true : false;
    }

    const getAverageRating = (reviews: Review[]) => {
        return (reviews.reduce((acc, curr) => acc + curr.rating, 0) / reviews.length) 
    }

    const searchFn = (params: searchParams) => {
        setSearchParams(params);
    }

    return (
        <div>
            <HeroContent /> 
            <Search searchFn={searchFn} />
            <FeaturedParks allParks={parks.filter((park) => isNear(park.location, searchParams.location) && getAverageRating(park.reviews) > searchParams.rating)} />
        </div>
    )
}