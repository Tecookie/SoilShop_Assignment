import { Icon } from "./Icon";
export function Notice({ error, text }: { error?: string; text?: string }) {
  return (
    <>
      {error && (
        <div className="notice error" role="alert">
          <Icon name="alert" />
          <span>{error}</span>
        </div>
      )}
      {text && (
        <div className="notice success" role="status">
          <Icon name="check" />
          <span>{text}</span>
        </div>
      )}
    </>
  );
}
