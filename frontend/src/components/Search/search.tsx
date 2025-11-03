import { useState } from "react"

export type searchParams = {
    location: string
    rating: number
}

type searchProps = {
    searchFn: (params: searchParams) => void
}

/*
This tool searches by location instead of name for a few reasons.
1. This is a mock discovery tool, so creating a tool that searches by name would hurt discovery
2. I believe location is a better metric to search on with regards to user experience (Short distance + most needs will likely be a better tradeoff than long distance for everything)
3. I think name based searching has a place in this, but I have two parks mocked and 2 weeks to make this, so I opted for what I consider the better option. 
*/

export default function Search(props: searchProps) {

    const [location, setLocation] = useState("")
    const [minimumRating, setMinimumRating] = useState(0);

    const { searchFn } = props;   

    const submitForm = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
        e.preventDefault()
        searchFn({location: location, rating: minimumRating});
    }
    return(
        <form className="flex container">
            <label className="location box">
                <div className="location text">
                    Location
                </div>
                <input className="location search-box" value={location} onChange={e => setLocation(e.target.value)} placeholder="Search by location..." />
            </label>
            <label className="rating box">
                <div className="rating text">
                    Minimum Rating
                </div>
                <select id="ratings" name="minRating" value={minimumRating} onChange={e => setMinimumRating(Number.parseFloat(e.target.value))}>
                    <option value={0}>Any Rating</option>
                    <option value={4.5}>4.5 Stars & Up</option>
                    <option value={4}>4 Stars & Up</option>
                    <option value={3}>3 Stars & Up</option>
                </select>
            </label>
            <button onClick={(e) => submitForm(e)}>Apply Filters</button>
        </form>
    )
}