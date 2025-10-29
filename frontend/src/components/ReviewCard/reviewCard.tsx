import Review from "../../models/review";
import './reviewCard.css';

interface reviewCardProps {
    review: Review;
}

export default function reviewCard(props: reviewCardProps) {
    const { review } = props;

    const formatDate = (val: Date) => {
        const options: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'long', day: 'numeric'}
        const newDate = new Date(val);
        return new Intl.DateTimeFormat('en-US', options).format(newDate);
    }

    const getStarRating = () => {
        const fullStar = <svg className="icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
        const emptyStar = <svg className="icon icon-empty" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
        const rating = review.rating
        const numStars = Math.floor(rating);
        const stars = []
        for(var i = 0; i < 5; i++) {
            if(i < numStars) {
                stars.push(fullStar);
            }
            else {
                stars.push(emptyStar);
            }
        }
        return <div className="parkCard ratings-block star-rating">{stars}</div>
    }
    return(
        <div className="reviewCard">
            <div className="left-column">
                <h3 className="review-creator">{review.author.displayName}</h3>
                <div className="date-container">
                    <div className="review-created">Written on {formatDate(review.dateWritten)}</div>
                    <div className="review-visited">Visited on {formatDate(review.dateVisited)}</div>
                </div>
                <hr className="border-line" />
                <div className="review">
                    {review.review}
                </div>
            </div>
            <div className="right-column">
                {getStarRating()}
            </div>
        </div>
    )
}