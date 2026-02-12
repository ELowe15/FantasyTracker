import React from "react";
import { ChevronUp } from "lucide-react";

interface ArrowToggleProps {
  open: boolean;
  onClick?: () => void;
  size?: number; // icon size in px
  colorClass?: string; // tailwind text color class
  className?: string;
}

export const ArrowToggle: React.FC<ArrowToggleProps> = ({
  open,
  onClick,
  size = 18,
  colorClass = "text-[var(--accent-primary)]",
  className = "",
}) => {
  return (
    <ChevronUp
      size={size}
      onClick={onClick}
      className={`
        cursor-pointer
        transition-transform
        duration-200
        ${colorClass}
        ${open ? "rotate-180" : ""}
        ${className}
      `}
    />
  );
};
