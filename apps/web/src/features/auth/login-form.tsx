import { Button } from "@/components/ui/button";
import { TextField } from "@/components/ui/text-field";

/** Disabled until authentication is implemented. */
export function LoginForm() {
  return (
    <form noValidate>
      <fieldset disabled className="grid gap-4">
        <legend className="sr-only">Your login details</legend>
        <TextField id="login-email" label="Email" type="email" autoComplete="email" />
        <TextField
          id="login-password"
          label="Password"
          type="password"
          autoComplete="current-password"
        />
        <Button type="submit" size="lg" className="mt-2 w-full">
          Log in
        </Button>
      </fieldset>
    </form>
  );
}
