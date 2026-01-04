import React from "react";

export default function RulesPage() {
  return (
    <div className="bg-slate-900 min-h-screen p-4 text-white">
      <h1 className="text-3xl font-bold text-center mb-6">
        Fantasy League Rules
      </h1>

      {/* Entry & Fee */}
      <section className="mb-6">
        <h2 className="text-xl font-semibold mb-2">Entry</h2>
        <p>Entry Fee: <span className="font-semibold">$40</span></p>
      </section>

      {/* Prize Money */}
      <section className="mb-6">
        <h2 className="text-xl font-semibold mb-2">Prize Money</h2>
        <ul className="list-disc list-inside space-y-1">
          <li>1st: <span className="font-semibold">$240</span></li>
          <li>2nd: <span className="font-semibold">$120</span></li>
          <li>3rd: <span className="font-semibold">$60</span></li>
          <li>Regular Season Leader: <span className="font-semibold">$60</span></li>
        </ul>
      </section>

      {/* Keepers */}
      <section className="mb-6">
        <h2 className="text-xl font-semibold mb-2">Keepers</h2>
        <ul className="list-disc list-inside space-y-1">
          <li>Max number of keepers: <span className="font-semibold">2</span></li>
          <li>Years you can keep a keeper: <span className="font-semibold">3 years</span> (includes this year)</li>
          <li>Can’t keep your first round pick</li>
          <li>You can choose to keep 0 or 1 keepers</li>
        </ul>
      </section>

      {/* Draft Picks */}
      <section className="mb-6">
        <h2 className="text-xl font-semibold mb-2">Draft & Trades</h2>
        <ul className="list-disc list-inside space-y-1">
          <li>Keepers will be your 2nd and 3rd round picks</li>
          <li>If you only have 1 keeper it's your 2nd round pick</li>
          <li>Can trade future draft picks</li>
          <li>Can trade keepers (still can’t keep a first round pick)</li>
        </ul>
      </section>
    </div>
  );
}
