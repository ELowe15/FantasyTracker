import React, { useState } from "react";
import TeamsPage from "./pages/TeamsPage";
import DraftResultsPage from "./pages/DraftResultsPage";
import RulesPage from "./pages/RulesPage";
import BestBallPage from "./pages/BestBallPage";
import RoundRobinPage from "./pages/RoundRobinPage";

type Tab = "teams" | "draft" | "rules" | "bestball" | "roundrobin";

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>("teams");

  return (
    <div className="bg-slate-900 min-h-screen w-full">
      {/* Fixed Top Navigation */}
      <nav className="fixed top-0 left-0 w-full h-12 bg-slate-800 text-white z-50 shadow-md">
  <div className="h-full flex justify-center items-center">
    {["teams", "draft", "bestball", "roundrobin", "rules"].map((tab, i) => (
      <React.Fragment key={tab}>
        {i > 0 && <span className="mx-1 text-gray-400 text-s">|</span>}
        <button
          className={`font-semibold text-xs px-1 ${
            activeTab === tab ? "underline" : ""
          }`}
          onClick={() => setActiveTab(tab as Tab)}
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
          px-3
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
