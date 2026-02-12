import React, { useState, useRef } from "react";
import TeamsPage from "./pages/TeamsPage";
import DraftResultsPage from "./pages/DraftResultsPage";
import RulesPage from "./pages/RulesPage";
import BestBallPage from "./pages/BestBallPage";
import RoundRobinPage from "./pages/RoundRobinPage";
import './styles/colors.css';

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
      className="min-h-screen w-full overflow-hidden"
      style={{ backgroundColor: "var(--bg-app)" }}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      {/* Fixed Top Navigation */}
      <nav
  className="fixed top-0 left-0 w-full h-12 z-50 shadow-md flex items-center px-3"
  style={{ backgroundColor: "var(--bg-nav)", color: "var(--text-primary)" }}
>
  {/* Logo / Favicon on the left */}
  <div className="flex items-center flex-shrink-0 mr-4">
    <img
      src="/favicon-32x32.png"       // path to your favicon or logo
      alt=""
      className="h-8 w-8 object-contain"
    />
  </div>

  {/* Tabs centered */}
  <div className="flex-grow flex justify-center items-center">
    {tabs.map((tab, i) => (
      <React.Fragment key={tab}>
        {i > 0 && (
          <span className="mx-1 text-s" style={{ color: "var(--text-divider)" }}>
            |
          </span>
        )}
        <button
          className={`font-semibold text-xs px-1`}
          style={{
            color:
              activeTab === tab ? "var(--text-active-tab)" : "var(--text-primary)",
            textDecoration: activeTab === tab ? "underline" : "none",
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
      <main className="pt-12 w-full">
        <div className="overflow-hidden w-full">
          <div
            className="flex"
            style={{
              transform: `translateX(calc(-${currentIndex * 100}% + ${dragOffset}px))`,
              transition: isDragging ? "none" : "transform 300ms ease-out",
            }}
          >
            <div
              className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5"
            >
              <TeamsPage />
            </div>
            <div
              className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5"
            >
              <DraftResultsPage />
            </div>
            <div
              className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5"
            >
              <BestBallPage />
            </div>
            <div
              className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5"
            >
              <RoundRobinPage />
            </div>
            <div
              className="w-full flex-shrink-0 px-1 sm:px-4 md:px-6 pb-5"
            >
              <RulesPage />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
