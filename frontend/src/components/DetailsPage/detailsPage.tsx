import IPark from "../../models/park";
import ReviewCard from "../ReviewCard/reviewCard";
import BookRide from "../BookRide/bookRide";
import './detailsPage.css';
import ParkService from "../../services/parkService";
import CartService from "../../services/cartService";

interface detailsPageProps {
    park: IPark
    parkService: ParkService
    cartService: CartService
    onBook: () => void
}

export default function DetailsPage(props: detailsPageProps) {
    const { park, cartService, onBook } = props;
 
    const getStarRating = () => {
        const fullStar = <svg className="icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
        const emptyStar = <svg className="icon icon-empty" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
        const averageRating = getAverageRating()
        const numStars = Math.floor(averageRating);
        const stars = []
        for(var i = 0; i < 5; i++) {
            if(i < numStars) {
                stars.push(fullStar);
            }
            else {
                stars.push(emptyStar);
            }
        }
        return <div className="detailsPage ratings-block stars">{stars}</div>
    }

    const getAverageRating = () => {
        if(park.reviews) {
            const sum = park.reviews.reduce((acc, curr) => acc + curr.rating, 0)
            return (sum / (park.reviews.filter((review) => review.active || true).length || 0));
        }
        return 0;
    }

    return (park && <div className="details-container">
            <img className="details-image" src={park.imageUrl} alt={park.parkName} />
            <div className="details-two-column">
                <div className="column details-left-column">
                    <h2 className="details-header">{park.parkName}</h2>
                    <div className="details-subheader">
                        <div className="location-subheader">
                            <svg className="icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"></path><circle cx="12" cy="10" r="3"></circle></svg>
                            <div className="location">{park.location}</div>
                        </div>
                        <div className="rating-block card">
                            <div className="star-rating">
                                {getStarRating()}
                            </div>
                            <div className="average-rating text">{getAverageRating().toFixed(2)} &#40;{park.reviews?.length || 0} Reviews&#41;</div>
                        </div>
                    </div>
                        <h3 className="park-details">About the Park</h3>
                        <hr className="border-line" / >
                        <div>
                            {park.description}
                        </div>
                        <h3 className="park-reviews">Recent Reviews</h3>
                        <hr className="border-line" />
                        {park.reviews?.map((review) => <ReviewCard review={review} />)}
                    </div>
                <div className="column details-right-column">
                    <BookRide park={park} cartService={cartService} onBook={onBook} />
                </div>
            </div>
        </div>)
}