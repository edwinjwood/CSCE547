import { useParams } from "react-router";
import DetailsPage from "../../components/DetailsPage/detailsPage";
import { useEffect, useState } from "react";
import IPark from "../../models/park";
import './parkDetails.css';
import ParkService from "../../services/parkService";
import CartService from "../../services/cartService";

interface ParkDetailsProps {
    parkService: ParkService
    cartService: CartService
    onBook: () => void
}

export default function ParkDetails(props: ParkDetailsProps) {
    const { parkId } = useParams();
    const { parkService, cartService, onBook } = props;
    const [selectedPark, setSelectedPark] = useState({} as IPark)

    useEffect(() => {
        parkService.getParkById(parkId).then((res) => {
            setSelectedPark(res);
        })
    }, [parkId, parkService])
    

    return (
        <div>
            <DetailsPage park={selectedPark} parkService={parkService} cartService={cartService} onBook={onBook}/>
        </div>
    )
}