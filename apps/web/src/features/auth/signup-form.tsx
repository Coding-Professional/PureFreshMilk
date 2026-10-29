import { Button } from "@/components/ui/button";
import { TextField } from "@/components/ui/text-field";

/** Disabled until authentication is implemented. */
export function SignupForm() {
  return (
    <form noValidate>
      <fieldset disabled className="grid gap-4">
        <legend className="sr-only">Your account details</legend>
        <TextField id="signup-name" label="Your name" autoComplete="name" />
        <TextField id="signup-email" label="Email" type="email" autoComplete="email" />
        <TextField
          id="signup-password"
          label="Password"
          type="password"
          autoComplete="new-password"
        />
        <Button type="submit" size="lg" className="mt-2 w-full">
          Create account
        </Button>
      </fieldset>
    </form>
  );
}
