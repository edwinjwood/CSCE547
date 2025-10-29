import './homebar.css';
import { useState } from 'react';

type HomebarProps = {
    numItems: number;
}

export default function Homebar(props: HomebarProps) {
    const title = "RideFinder"

    const { numItems } = props;

    const [darkTheme, setDarkTheme] = useState(false)

    const toggleTheme = () => {
        setDarkTheme(!darkTheme);
    }

    return (
        <div className="flex">
            <div className="left title inner-flex">
                <div className="title-arrow">
                    <svg className="logo-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2Z"/><path d="m15 12-3 3-3-3"/></svg>
                </div>
                <div className="title-text">
                    <a href="/">
                        {title}
                    </a>
                </div>
            </div>
            <div className="right inner-flex">
                <button className='theme-toggle' onClick={() => toggleTheme()} aria-label='Toggle theme'>
                    {darkTheme ? 
                        <svg className="icon moon-icon" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>
                        :
                        <svg className="icon sun-icon" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/></svg>
                    }
                </button>
                <div className="cart display">
                    {numItems > 0 && 
                    <div className="cartNumber">
                        {numItems}
                    </div>
                    }
                    <a href="/cart">
                        <svg className="icon" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="8" cy="21" r="1"/><circle cx="19" cy="21" r="1"/><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.16"/></svg>
                    </a>
                </div>
            </div>
        </div>
    )
}