import { createContext, type ReactNode } from "react";

export interface DetailPanelValue {
  openDetailPanel: (title: string, content: ReactNode) => void;
  closeDetailPanel: () => void;
  title: string;
  content: ReactNode;
}

const defaultValue: DetailPanelValue = {
  openDetailPanel: () => {},
  closeDetailPanel: () => {},
  title: "",
  content: null,
};

const DetailPanelContext = createContext<DetailPanelValue>(defaultValue);

export default DetailPanelContext;
