import { useContext } from "react";

import SideDrawerContext from "./side-drawer-context";

const useSideDrawer = () => useContext(SideDrawerContext);

export default useSideDrawer;
