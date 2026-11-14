import type { ReactNode } from "react";
import { Icon } from "./Icon";
export function Empty({ children }: { children: ReactNode }) {
  return (
    <div className="empty" role="status">
      <span className="empty-symbol">
        <Icon name="search" />
      </span>
      <span>{children}</span>
    </div>
  );
}
