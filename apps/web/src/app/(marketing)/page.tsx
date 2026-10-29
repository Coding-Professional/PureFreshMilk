import { Hero } from "@/features/landing/hero";
import { MorningSteps } from "@/features/landing/morning-steps";
import { ProjectScope } from "@/features/landing/project-scope";

export default function LandingPage() {
  return (
    <>
      <Hero />
      <MorningSteps />
      <ProjectScope />
    </>
  );
}
