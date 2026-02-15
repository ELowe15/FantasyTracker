// DelayedLoader.tsx
import { useEffect, useState, ReactNode } from "react";

interface DelayedLoaderProps {
  loading: boolean;
  dataLoaded: boolean;
  error: string | null;
  message?: string;
  children: ReactNode;
  delay?: number; // milliseconds before showing loading
}

export function DelayedLoader({
  loading,
  dataLoaded,
  error,
  message = "Loading...",
  children,
  delay = 400, // default 400ms
}: DelayedLoaderProps) {
  const [showLoading, setShowLoading] = useState(false);

  useEffect(() => {
    let timer: NodeJS.Timeout;

    if (loading) {
      timer = setTimeout(() => {
        setShowLoading(true);
      }, delay);
    } else {
      setShowLoading(false);
    }

    return () => clearTimeout(timer);
  }, [loading, delay]);

  if (error) {
    return (
      <div className="flex justify-center items-center h-24" style={{ color: "var(--accent-error)" }}>
        {error}
      </div>
    );
  }

  if (showLoading && !dataLoaded) {
    return (
      <div className="flex justify-center items-center h-24" style={{ color: "var(--text-secondary)" }}>
        {message}
      </div>
    );
  }

  return <>{children}</>;
}
