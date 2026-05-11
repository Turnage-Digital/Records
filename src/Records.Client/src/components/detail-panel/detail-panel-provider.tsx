import * as React from "react";
import {
  type PropsWithChildren,
  type ReactNode,
  useMemo,
  useState,
} from "react";

import DetailPanelContext from "./detail-panel-context";

const DetailPanelProvider = ({ children }: PropsWithChildren) => {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState<ReactNode>(null);

  const openDetailPanel = (title: string, content: ReactNode): void => {
    setTitle(title);
    setContent(content);
  };

  const closeDetailPanel = (): void => {
    setContent(null);
  };

  const value = useMemo(
    () => ({
      openDetailPanel,
      closeDetailPanel,
      title,
      content,
    }),
    [content, title],
  );

  return (
    <DetailPanelContext.Provider value={value}>
      {children}
    </DetailPanelContext.Provider>
  );
};

export default DetailPanelProvider;
