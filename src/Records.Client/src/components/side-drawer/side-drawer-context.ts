import { createContext, ReactNode } from "react";

export interface SideDrawerValue {
  openDrawer: (title: string, content: ReactNode) => void;
  closeDrawer: () => void;
  title: string;
  content: ReactNode;
}

const defaultValue: SideDrawerValue = {
  openDrawer: () => {},
  closeDrawer: () => {},
  title: "",
  content: null,
};

const SideDrawerContext = createContext<SideDrawerValue>(defaultValue);

export default SideDrawerContext;
