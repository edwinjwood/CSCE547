import IPark from "../../models/park";
import './parkCard.css'

export function ParkCard({park}: {park: IPark }) {

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
        return <div className="parkCard ratings-block stars">{stars}</div>
    }

    const getAverageRating = () => {
        const sum = park.reviews.reduce((acc, curr) => acc + curr.rating, 0)
        return (sum / (park.reviews.filter((review) => review.active || true).length || 0));
    }

    return(
        <div className="parkCard card-container">
            <a href={`/details/${park.id}`}> 
                <img src={park.imageUrl} alt={park.parkName} className="parkCard image-park"/>
            </a>
            <div className="parkCard card-text">
                <h2> {park.parkName} </h2>
                <div className="parkCard location-container">
                    <svg className="icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"></path><circle cx="12" cy="10" r="3"></circle></svg>
                    <div>{park.location}</div>
                </div>
                <div className="parkCard rating-block card">
                    <div className="parkCard star-rating">
                        {getStarRating()}
                    </div>
                    <div className="parkCard average-rating text">{getAverageRating().toFixed(2)} &#40;{park.reviews.length} Reviews&#41;</div>
                </div>
                <div>
                    {park.description}
                </div>
                <a href={`/details/${park.id}`}>
                    <button className="button-primary button-book">Book Now</button>
                </a>
            </div>
        </div>
    )
}