import React, { useState } from "react";
import TeamsPage from "./pages/TeamsPage";
import DraftResultsPage from "./pages/DraftResultsPage";
import RulesPage from "./pages/RulesPage";
import BestBallPage from "./pages/BestBallPage";
import RoundRobinPage from "./pages/RoundRobinPage";

type Tab = "teams" | "draft" | "bestball" | "roundrobin" | "rules";

export default function App() {
  const tabs: Tab[] = ["teams", "draft", "bestball", "roundrobin", "rules"];
  const [activeTab, setActiveTab] = useState<Tab>("bestball");

  // For swipe detection
  let touchStartX = 0;
  let touchEndX = 0;

  const handleTouchStart = (e: React.TouchEvent) => {
    touchStartX = e.changedTouches[0].screenX;
  };

  const handleTouchEnd = (e: React.TouchEvent) => {
    touchEndX = e.changedTouches[0].screenX;
    handleSwipeGesture();
  };

  const handleSwipeGesture = () => {
    const threshold = 50; // minimum swipe distance in px
    if (Math.abs(touchEndX - touchStartX) < threshold) return;

    const currentIndex = tabs.indexOf(activeTab);
    if (touchEndX < touchStartX) {
      // Swipe left → go to next tab
      if (currentIndex < tabs.length - 1) {
        setActiveTab(tabs[currentIndex + 1]);
      }
    } else if (touchEndX > touchStartX) {
      // Swipe right → go to previous tab
      if (currentIndex > 0) {
        setActiveTab(tabs[currentIndex - 1]);
      }
    }
  };

  return (
    <div
      className="bg-slate-900 min-h-screen w-full"
      onTouchStart={handleTouchStart}
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

      {/* Main content */}
      <main
        className="
          pt-12
          px-1
          pb-3
          sm:px-4 sm:pb-4
          md:px-6 md:pb-5
          max-w-6xl
          mx-auto
          w-full
        "
      >
        {activeTab === "teams" && <TeamsPage />}
        {activeTab === "draft" && <DraftResultsPage />}
        {activeTab === "bestball" && <BestBallPage />}
        {activeTab === "roundrobin" && <RoundRobinPage />}
        {activeTab === "rules" && <RulesPage />}
      </main>
    </div>
  );
}
