import { useContext } from "react";

import DetailPanelContext from "./detail-panel-context";

const useDetailPanel = () => useContext(DetailPanelContext);

export default useDetailPanel;
