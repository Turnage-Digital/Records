import * as React from "react";
import { PropsWithChildren, ReactNode, useMemo, useState } from "react";

import SideDrawerContext from "./side-drawer-context";

const SideDrawerProvider = ({ children }: PropsWithChildren) => {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState<ReactNode>(null);

  const openDrawer = (title: string, content: ReactNode): void => {
    setTitle(title);
    setContent(content);
  };

  const closeDrawer = (): void => {
    setContent(null);
  };

  const value = useMemo(
    () => ({
      openDrawer,
      closeDrawer,
      title,
      content,
    }),
    [content, title],
  );

  return (
    <SideDrawerContext.Provider value={value}>
      {children}
    </SideDrawerContext.Provider>
  );
};

export default SideDrawerProvider;
