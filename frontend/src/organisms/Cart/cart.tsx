import CartDetails from "../../components/CartDetails/cartDetails";
import CartService from "../../services/cartService";

type CartProps = {
	cartService: CartService
    handleChange: () => void
};

export default function Cart(props: CartProps) {
    const { cartService, handleChange } = props;
	return (
        <div>
            <CartDetails cartService={cartService} handleDelete={handleChange}/>
        </div>
    )
}