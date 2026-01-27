import React, { useState, useRef } from "react";
import TeamsPage from "./pages/TeamsPage";
import DraftResultsPage from "./pages/DraftResultsPage";
import RulesPage from "./pages/RulesPage";
import BestBallPage from "./pages/BestBallPage";
import RoundRobinPage from "./pages/RoundRobinPage";

type Tab = "teams" | "draft" | "bestball" | "roundrobin" | "rules";

export default function App() {
  const tabs: Tab[] = ["teams", "draft", "bestball", "roundrobin", "rules"];
  const [activeTab, setActiveTab] = useState<Tab>("bestball");

  const touchStartX = useRef(0);
  const [dragOffset, setDragOffset] = useState(0);
  const [isDragging, setIsDragging] = useState(false);

  const currentIndex = tabs.indexOf(activeTab);

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX.current = e.touches[0].screenX;
    setIsDragging(true);
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (!isDragging) return;
    const currentX = e.touches[0].screenX;
    setDragOffset(currentX - touchStartX.current);
  };

  const handleTouchEnd = () => {
    const threshold = 80;

    if (dragOffset < -threshold && currentIndex < tabs.length - 1) {
      setActiveTab(tabs[currentIndex + 1]);
    } else if (dragOffset > threshold && currentIndex > 0) {
      setActiveTab(tabs[currentIndex - 1]);
    }

    setDragOffset(0);
    setIsDragging(false);
  };

  return (
    <div
      className="bg-slate-900 min-h-screen w-full overflow-hidden"
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      {/* Fixed Top Navigation */}
      <nav className="fixed top-0 left-0 w-full h-12 bg-slate-800 text-white z-50 shadow-md">
        <div className="h-full flex justify-center items-center">
          {tabs.map((tab, i) => (
            <React.Fragment key={tab}>
              {i > 0 && <span className="mx-1 text-gray-400 text-s">|</span>}
              <button
                className={`font-semibold text-xs px-1 ${
                  activeTab === tab ? "underline" : ""
                }`}
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
      <main className="pt-12 w-full">
        <div className="overflow-hidden w-full">
          <div
            className="flex"
            style={{
              transform: `translateX(calc(-${currentIndex * 100}% + ${dragOffset}px))`,
              transition: isDragging
                ? "none"
                : "transform 300ms ease-out",
            }}
          >
            <div className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5">
              <TeamsPage />
            </div>
            <div className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5">
              <DraftResultsPage />
            </div>
            <div className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5">
              <BestBallPage />
            </div>
            <div className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5">
              <RoundRobinPage />
            </div>
            <div className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5">
              <RulesPage />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
