import { ParkCard } from '../parkCard/parkCard';
import IPark from '../../models/park';
import './featuredParks.css';

interface FeaturedParksProps {
    allParks: IPark[]
}

export default function FeaturedParks(props: FeaturedParksProps) {
    const { allParks } = props;

    return(
        <>
            <h2>Featured Parks</h2>
            <div className="featured-park-grid">
                {allParks.map((park: IPark) => {
                    return <ParkCard key={`ParkID:${park.id}`} park={park} />
                })}
            </div>
        </>
    )
}