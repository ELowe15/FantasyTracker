import React, { useState, useEffect, useRef } from "react";
import TeamsPage from "./pages/TeamsPage";
import DraftResultsPage from "./pages/DraftResultsPage";
import RulesPage from "./pages/RulesPage";
import BestBallPage from "./pages/BestBallPage";
import RoundRobinPage from "./pages/RoundRobinPage";
import "./styles/colors.css";
import { loadPlayerImages } from "./services/playerImageService";
import { usePersistentState } from "./hooks/usePersistentState";


type Tab = "teams" | "draft" | "bestball" | "roundrobin" | "rules";

export default function App() {
  const tabs: Tab[] = ["teams", "draft", "bestball", "roundrobin", "rules"];
const [activeTab, setActiveTab] =
  usePersistentState<Tab>("app:activeTab", "bestball");
  const containerRef = useRef<HTMLDivElement | null>(null);

  const touchStartX = useRef(0);
  const touchStartY = useRef(0);
  const isHorizontalGesture = useRef<boolean | null>(null);

  const [dragOffset, setDragOffset] = useState(0);
  const [isDragging, setIsDragging] = useState(false);

  const panelRefs = useRef<(HTMLDivElement | null)[]>([]);

  const currentIndex = tabs.indexOf(activeTab);

  useEffect(() => {
    loadPlayerImages();
  }, []);

  // Reset vertical scroll when tab changes
  useEffect(() => {
    const index = tabs.indexOf(activeTab);
    panelRefs.current[index]?.scrollTo({ top: 0 });
  }, [activeTab]);

  // Attach NON-PASSIVE touchmove listener
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const handleTouchMove = (e: TouchEvent) => {
      if (!isDragging) return;

      const touch = e.touches[0];
      const deltaX = touch.clientX - touchStartX.current;
      const deltaY = touch.clientY - touchStartY.current;

      // Determine direction
      if (isHorizontalGesture.current === null) {
        if (Math.abs(deltaX) > 8 || Math.abs(deltaY) > 8) {
          isHorizontalGesture.current =
            Math.abs(deltaX) > Math.abs(deltaY);
        }
      }

      if (isHorizontalGesture.current) {
        e.preventDefault(); // This now ACTUALLY works
        setDragOffset(deltaX);
      }
    };

    el.addEventListener("touchmove", handleTouchMove, {
      passive: false,
    });

    return () => {
      el.removeEventListener("touchmove", handleTouchMove);
    };
  }, [isDragging]);

  const handleTouchStart = (e: React.TouchEvent) => {
    const touch = e.touches[0];
    touchStartX.current = touch.clientX;
    touchStartY.current = touch.clientY;
    isHorizontalGesture.current = null;
    setIsDragging(true);
  };

  const handleTouchEnd = () => {
    if (!isHorizontalGesture.current) {
      setIsDragging(false);
      return;
    }

    const threshold = 80;

    if (dragOffset < -threshold && currentIndex < tabs.length - 1) {
      setActiveTab(tabs[currentIndex + 1]);
    } else if (dragOffset > threshold && currentIndex > 0) {
      setActiveTab(tabs[currentIndex - 1]);
    }

    setDragOffset(0);
    setIsDragging(false);
    isHorizontalGesture.current = null;
  };

  return (
    <div
      ref={containerRef}
      className="h-screen w-full overflow-hidden"
      style={{ backgroundColor: "var(--bg-app)" }}
      onTouchStart={handleTouchStart}
      onTouchEnd={handleTouchEnd}
    >
      {/* Fixed Top Navigation */}
      <nav
        className="fixed top-0 left-0 w-full h-12 z-50 shadow-md flex items-center justify-center px-3"
        style={{
          backgroundColor: "var(--bg-nav)",
          color: "var(--text-primary)",
        }}
      >
        <div className="flex space-x-2">
          {tabs.map((tab, i) => (
            <React.Fragment key={tab}>
              {i > 0 && (
                <span
                  className="mx-1 text-s"
                  style={{ color: "var(--text-divider)" }}
                >
                  |
                </span>
              )}
              <button
                className="font-semibold text-xs px-1"
                style={{
                  color:
                    activeTab === tab
                      ? "var(--text-active-tab)"
                      : "var(--text-primary)",
                  textDecoration:
                    activeTab === tab ? "underline" : "none",
                }}
                onClick={() => setActiveTab(tab)}
              >
                {tab === "teams"
                  ? "Teams"
                  : tab === "draft"
                  ? "Draft"
                  : tab === "bestball"
                  ? "Best Ball"
                  : tab === "roundrobin"
                  ? "Round Robin"
                  : "Rules"}
              </button>
            </React.Fragment>
          ))}
        </div>
      </nav>

      {/* Sliding Content */}
      <main className="pt-12 h-[calc(100vh)] w-full overflow-hidden">
        <div className="h-full w-full overflow-hidden">
          <div
            className="flex h-full"
            style={{
              transform: `translateX(calc(-${currentIndex * 100}% + ${dragOffset}px))`,
              transition: isDragging
                ? "none"
                : "transform 300ms ease-out",
            }}
          >
            {/* Teams */}
            <div
              ref={(el) => {(panelRefs.current[0] = el)}}
              className="w-full flex-shrink-0 h-full overflow-y-auto px-1 sm:px-4 md:px-6 pb-5"
            >
              <TeamsPage />
            </div>

            {/* Draft */}
            <div
              ref={(el) => {(panelRefs.current[1] = el)}}
              className="w-full flex-shrink-0 h-full overflow-y-auto px-1 sm:px-4 md:px-6 pb-5"
            >
              <DraftResultsPage />
            </div>

            {/* Best Ball */}
            <div
              ref={(el) => {(panelRefs.current[2] = el)}}
              className="w-full flex-shrink-0 h-full overflow-y-auto px-1 sm:px-4 md:px-6 pb-5"
            >
              <BestBallPage />
            </div>

            {/* Round Robin */}
            <div
              ref={(el) => {(panelRefs.current[3] = el)}}
              className="w-full flex-shrink-0 h-full overflow-y-auto px-1 sm:px-4 md:px-6 pb-5"
            >
              <RoundRobinPage />
            </div>

            {/* Rules */}
            <div
              ref={(el) => {(panelRefs.current[4] = el)}}
              className="w-full flex-shrink-0 h-full overflow-y-auto px-1 sm:px-4 md:px-6 pb-5"
            >
              <RulesPage />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
