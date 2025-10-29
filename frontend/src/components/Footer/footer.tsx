import './footer.css'

export default function Footer() {
    const currentYear = new Date().getFullYear();
    const title = "RideFinder"
    return (
        <div className="footer container" title='This is not legally binding, I just wanted it to look official'>&copy; {currentYear} {title}. All Rights Reserved.</div>
    )
}