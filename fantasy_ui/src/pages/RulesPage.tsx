import React, { useState } from "react";

type CollapsibleSectionProps = {
  title: string;
  children: React.ReactNode;
};

function CollapsibleSection({ title, children }: CollapsibleSectionProps) {
  const [open, setOpen] = useState(false);

  return (
    <section className="mb-6 border border-slate-700 rounded-lg">
      <button
        onClick={() => setOpen(!open)}
        className="w-full flex items-center justify-between px-4 py-3 text-left"
      >
        <h2 className="text-xl font-semibold">{title}</h2>
        <span
          className={`transform transition-transform duration-200 ${
            open ? "rotate-90" : ""
          }`}
        >
          ▶
        </span>
      </button>

      {open && <div className="px-4 pb-4">{children}</div>}
    </section>
  );
}

export default function RulesPage() {
  return (
    <div className="bg-slate-900 min-h-screen p-4 text-white">
      <h1 className="text-3xl font-bold text-center mb-6">
        Fantasy League Rules
      </h1>

      {/* Entry & Prize Money */}
      <CollapsibleSection title="Entry & Prize Money">
        <section className="mb-4">
          <h3 className="text-lg font-semibold mb-2">Entry</h3>
          <p>
            Entry Fee: <span className="font-semibold">$40</span>
          </p>
        </section>

        <section>
          <h3 className="text-lg font-semibold mb-2">Prize Money</h3>
          <ul className="list-disc list-inside space-y-1">
            <li>
              1st: <span className="font-semibold">$240</span>
            </li>
            <li>
              2nd: <span className="font-semibold">$120</span>
            </li>
            <li>
              3rd: <span className="font-semibold">$60</span>
            </li>
            <li>
              Regular Season Leader:{" "}
              <span className="font-semibold">$60</span>
            </li>
          </ul>
        </section>
      </CollapsibleSection>

      {/* Keepers */}
      <CollapsibleSection title="Keepers">
        <ul className="list-disc list-inside space-y-1">
          <li>
            Number of keepers: <span className="font-semibold">0, 1, or 2</span>
          </li>
          <li>
            Years you can keep a keeper:{" "}
            <span className="font-semibold">3 years</span> (includes this year)
          </li>
          <li>Can’t keep your first round pick</li>
        </ul>
      </CollapsibleSection>

      {/* Draft & Trades */}
      <CollapsibleSection title="Draft & Trades">
        <ul className="list-disc list-inside space-y-1">
          <li>Keepers will be your 2nd and 3rd round picks</li>
          <li>If you only have 1 keeper it's your 3rd round pick</li>
          <li>Can trade future draft picks</li>
          <li>Can trade keepers (still can’t keep a first round pick)</li>
        </ul>
      </CollapsibleSection>

            {/* Best Ball Rules */}
      <CollapsibleSection title="Best Ball Rules">
        <ul className="list-disc list-inside space-y-2 mb-4">
          <li>
            Best Ball scoring is calculated automatically each week based on
            your roster’s performance.
          </li>
          <li>
            The system counts the best possible combination of games to form
            your weekly lineup.
          </li>
          <li>
            All scoring uses{" "}
            <span className="font-semibold">
              standard Yahoo fantasy basketball category scoring
            </span>
            .
          </li>
          <li>
            <span className="font-semibold">Important Yahoo limitation:</span>{" "}
            Games played by players who are on your{" "}
            <span className="font-semibold">bench or IL</span> do{" "}
            <span className="font-semibold">not</span> count toward Best Ball
            scoring.
          </li>
          <li>
            This is a limitation of the Yahoo API and cannot be changed at this
            time.
          </li>
        </ul>

        <h3 className="text-lg font-semibold mb-2">
  Yahoo Fantasy Points (Best Ball)
</h3>
<p className="mb-2 text-sm text-slate-300">
  Each player on your roster contributes points automatically. The points are assigned as follows:
</p>
<ul className="list-disc list-inside space-y-1">
  <li>
    <span className="font-semibold">Points (PTS):</span> 1 point per point scored
  </li>
  <li>
    <span className="font-semibold">Rebounds (REB):</span> 1.2 points per rebound
  </li>
  <li>
    <span className="font-semibold">Assists (AST):</span> 1.5 points per assist
  </li>
  <li>
    <span className="font-semibold">Steals (STL):</span> 3 points per steal
  </li>
  <li>
    <span className="font-semibold">Blocks (BLK):</span> 3 points per block
  </li>
  <li>
    <span className="font-semibold">Turnovers (TO):</span> -1 point per turnover
  </li>
</ul>

      </CollapsibleSection>

      {/* Round Robin Rules */}
      <CollapsibleSection title="Round Robin Rules">
        <ul className="list-disc list-inside space-y-2">
          <li>
            Round Robin means you are effectively playing{" "}
            <span className="font-semibold">every team</span> in the league each
            week.
          </li>
          <li>
            Each Round Robin matchup is scored as a{" "}
            <span className="font-semibold">standard 9-category week</span>.
          </li>
          <li>
            If your team wins the 9-category matchup against another team, you
            earn a <span className="font-semibold">Round Robin win</span> against
            that team.
          </li>
          <li>
            You will have multiple wins and losses in a single week depending
            on how your team performs relative to the rest of the league.
          </li>
          <li>
            Season standings are based on your total Round Robin wins, losses,
            and ties, not a single head-to-head opponent.
          </li>
        </ul>
      </CollapsibleSection>
    </div>
  );
}
