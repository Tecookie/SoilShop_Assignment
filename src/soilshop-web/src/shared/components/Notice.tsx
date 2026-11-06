export function Notice({ error, text }: { error?: string; text?: string }) {
  return (
    <>
      {error && (
        <div className="notice error" role="alert">
          {error}
        </div>
      )}
      {text && (
        <div className="notice success" role="status">
          {text}
        </div>
      )}
    </>
  );
}
