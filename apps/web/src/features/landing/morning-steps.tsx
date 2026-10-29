// A real sequence, so the steps are numbered.
const steps = [
  {
    title: "The milk arrives",
    body: "Open Today. Everyone who gets milk is already listed with their usual quantity.",
  },
  {
    title: "Tap what happened",
    body: "Delivered or missed, for each person. If more or less than usual was left, change the quantity.",
  },
  {
    title: "Check the measure",
    body: "The day's measure fills as you go, so a short delivery or a missed doorstep stands out straight away.",
  },
];

export function MorningSteps() {
  return (
    <section
      id="how-it-works"
      aria-labelledby="how-heading"
      className="scroll-mt-6 border-t border-line bg-surface"
    >
      <div className="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:px-8 lg:py-20">
        <h2 id="how-heading" className="text-3xl font-extrabold sm:text-4xl">
          How a morning goes
        </h2>
        <ol className="mt-10 grid gap-8 md:grid-cols-3 md:gap-10">
          {steps.map((step, index) => (
            <li key={step.title} className="border-t-2 border-ink pt-5">
              <p className="font-display text-sm font-bold text-accent">Step {index + 1}</p>
              <h3 className="mt-2 text-xl font-bold">{step.title}</h3>
              <p className="mt-2 leading-relaxed text-ink-muted">{step.body}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
