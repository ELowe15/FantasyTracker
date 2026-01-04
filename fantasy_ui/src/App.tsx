import React, { useState } from "react";
import TeamsPage from "./pages/TeamsPage";
import DraftResultsPage from "./pages/DraftResultsPage";
import RulesPage from "./pages/RulesPage";
import BestBallPage from "./pages/BestBallPage";

type Tab = "teams" | "draft" | "rules" | "bestball";

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>("teams");

  return (
    <div className="bg-slate-900 min-h-screen w-full">
      {/* Fixed Top Navigation */}
      <nav className="fixed top-0 left-0 w-full bg-slate-900 text-white z-50 p-4 shadow-md">
        <div className="flex justify-center space-x-6">
          <button
            className={`font-semibold text-lg ${activeTab === "teams" ? "underline" : ""}`}
            onClick={() => setActiveTab("teams")}
          >
            Teams
          </button>
          <button
            className={`font-semibold text-lg ${activeTab === "draft" ? "underline" : ""}`}
            onClick={() => setActiveTab("draft")}
          >
            Draft
          </button>
          <button
            className={`font-semibold text-lg ${activeTab === "bestball" ? "underline" : ""}`}
            onClick={() => setActiveTab("bestball")}
          >
            Best Ball
          </button>
          <button
            className={`font-semibold text-lg ${activeTab === "rules" ? "underline" : ""}`}
            onClick={() => setActiveTab("rules")}
          >
            Rules
          </button>
        </div>
      </nav>

      {/* Main content */}
      <main className="pt-20 p-2 sm:p-4 md:p-6 max-w-6xl mx-auto w-full">
        {activeTab === "teams" && <TeamsPage />}
        {activeTab === "draft" && <DraftResultsPage />}
        {activeTab === "bestball" && <BestBallPage />}
        {activeTab === "rules" && <RulesPage />}
      </main>
    </div>
  );
}
