# Dice Roll Mini-Language

Keywords are case-insensitive.

## 1. Lexicography

### Characters

```ebnf
digit
    := "0"…"9" ;

positive_digit
    := "1"…"9" ;
```

### Numbers

```ebnf
count
    := positive_digit digit* ;

integer
    := "-"? digit+ ;
```

### Operators and punctuation

```ebnf
add_op
    := "+"
     | "-" ;

mul_op
    := "*"
     | "/"
     | "//" ;
```

### Dice pools

```ebnf
pool
    := count "d" count ;
```

Whitespace around `d` is not permitted.

### Keywords

```ebnf
kw_highest
    := "highest"
     | "high"
     | "h" ;

kw_lowest
    := "lowest"
     | "low"
     | "l" ;

kw_keep_highest
    := kw_keep kw_highest ;

kw_keep_lowest
    := kw_keep kw_lowest ;

kw_drop
    := "drop"
     | "d" ;

kw_drop_highest
    := kw_drop kw_highest ;

kw_drop_lowest
    := kw_drop kw_lowest ;

kw_reroll
    := "reroll"
     | "rr" ;

kw_keep
    := "keep"
	 | "k" ;

kw_then
    := "then" ;

kw_of
    := "of" ;

kw_sum
    := "sum"
     | "s" ;

kw_product
    := "product"
     | "prod"
     | "p" ;
```

## 2. Whitespace and Token Joining

Whitespace may occur between any two tokens unless a production explicitly requires adjacency.

Whitespace may be removed between adjacent tokens only when the resulting source still has a unique tokenization into the same tokens.

### Required whitespace

Whitespace is required when removing it would:

* merge two numbers,
* change the intended tokenization,
* or produce an undefined token.

### Symbolic boundaries

Whitespace is optional around:

`(` `)` `+` `-` `*` `/` `//`

## 3. Expression Grammar

```ebnf
expression
    := additive ;

additive
    := multiplicative
       { add_op multiplicative } ;

multiplicative
    := primary
       { mul_op primary } ;

primary
    := roll
     | aggregate
     | integer
     | parenthesized ;

parenthesized
    := "(" expression ")" ;
```

The expression is the top-level unit.

## 4. Precedence

Without parentheses, precedence from highest to lowest is:

```text
primary
* / //
+ -
```

Operators at the same precedence level associate left-to-right.

Parentheses override this ordering arbitrarily.

## 5. Rolls

```ebnf
roll
    := pool roll_chain ;

roll_chain
    := roll_step
       { kw_then? roll_step }
     | ;

roll_step
    := reroll_step
     | keep_selector
     | drop_selector ;
```

A roll consists of rolling a pool of dice and then applying zero or more roll steps from left to right.

## 6. Keep and Drop Selectors

Keep and drop selectors are postfix roll steps:

```ebnf
keep_selector
    := kw_keep_highest count?
     | kw_keep_lowest count? ;

drop_selector
    := kw_drop_highest count?
     | kw_drop_lowest count? ;
```

A missing selector count always defaults to `1`.

## 7. Rerolls

Rerolls are postfix roll steps with their own selector grammar:

```ebnf
reroll_selector
    := reroll_highest
     | reroll_lowest ;

reroll_highest
    := kw_highest count? ;

reroll_lowest
    := kw_lowest count? ;

reroll_step
    := kw_reroll reroll_selector ;
```

A missing selector count always defaults to `1`.

## 8. Aggregates

```ebnf
aggregate
    := sum_aggregate
     | product_aggregate ;

sum_aggregate
    := kw_sum kw_of? count primary ;

product_aggregate
    := kw_product kw_of? count primary ;
```

The operand of an aggregate is exactly one `primary`.

Parentheses therefore allow aggregates to operate on arbitrarily complex expressions.

## 9. Arbitrary Ordering with Parentheses

All grouping is expressed through the same recursive rule:

```ebnf
primary
    := roll
     | aggregate
     | integer
     | parenthesized ;
```

Because a parenthesized `expression` becomes a `primary`, it can appear wherever any other primary is allowed.

## 10. Evaluation Semantics

### Rolls

Roll the complete pool, then evaluate each roll step from left to right down the chain, with each step modifying the pool.

After the chain is exhausted, sum the dice remaining in the pool to produce the roll value.

### Aggregates

For an aggregate of count `N`, its `primary` operand is independently evaluated `N` times.

`sum` adds those results.

`product` multiplies those results.

### Division

```text
/   real-valued division
//  integer division truncated toward zero
```

Division by zero is an evaluation error.

# Complete grammar

```ebnf
digit
    := "0"…"9" ;

positive_digit
    := "1"…"9" ;

count
    := positive_digit digit* ;

integer
    := "-"? digit+ ;

add_op
    := "+"
     | "-" ;

mul_op
    := "*"
     | "/"
     | "//" ;

kw_highest
    := "highest"
     | "high"
     | "h" ;

kw_lowest
    := "lowest"
     | "low"
     | "l" ;

kw_keep
    := "keep"
     | "k" ;

kw_keep_highest
    := kw_keep kw_highest ;

kw_keep_lowest
    := kw_keep kw_lowest ;

kw_drop
    := "drop"
     | "d" ;

kw_drop_highest
    := kw_drop kw_highest ;

kw_drop_lowest
    := kw_drop kw_lowest ;

kw_reroll
    := "reroll"
     | "rr" ;

kw_then
    := "then" ;

kw_of
    := "of" ;

kw_sum
    := "sum"
     | "s" ;

kw_product
    := "product"
     | "prod"
     | "p" ;

pool
    := count "d" count ;

keep_selector
    := kw_keep_highest count?
     | kw_keep_lowest count? ;

drop_selector
    := kw_drop_highest count?
     | kw_drop_lowest count? ;

reroll_highest
    := kw_highest count? ;

reroll_lowest
    := kw_lowest count? ;

reroll_selector
    := reroll_highest
     | reroll_lowest ;

reroll_step
    := kw_reroll reroll_selector ;

roll_step
    := reroll_step
     | keep_selector
     | drop_selector ;

roll_chain
    := roll_step
       { kw_then? roll_step }
     | ;

roll
    := pool roll_chain ;

sum_aggregate
    := kw_sum kw_of? count primary ;

product_aggregate
    := kw_product kw_of? count primary ;

aggregate
    := sum_aggregate
     | product_aggregate ;

parenthesized
    := "(" expression ")" ;

primary
    := roll
     | aggregate
     | integer
     | parenthesized ;

multiplicative
    := primary
       { mul_op primary } ;

additive
    := multiplicative
       { add_op multiplicative } ;

expression
    := additive ;
```

# Examples

| Examples                                                 | Compact form                    | Expanded / equivalent forms                                             |
| -------------------------------------------------------- | ------------------------------- | ----------------------------------------------------------------------- |
| Attack roll with +5 modifier                             | `1d20+5`                        | `1d20 + 5`                                                              |
| Attack roll with +5 modifier with advantage              | `2d20kh+5`                      | `2d20 keep highest + 5`                                                 |
| Attack roll with +5 modifier with disadvantage           | `2d20kl+5`                      | `2d20 keep lowest + 5`                                                  |
| Elven Accuracy attack roll                               | `2d20rrlkh+5`<br>≡ `3d20kh+5`   | `2d20 reroll lowest then keep highest + 5`<br>≡ `3d20 keep highest + 5` |
| Keep highest 3 of 4d6                                    | `4d6kh3`                        | `4d6 keep highest 3`                                                    |
| Keep lowest 2 of 4d6                                     | `4d6kl2`                        | `4d6 keep lowest 2`                                                     |
| Drop highest die from 4d6                                | `4d6dh`                         | `4d6 drop highest`                                                      |
| Drop lowest die from 4d6                                 | `4d6dl`                         | `4d6 drop lowest`<br>≡ `4d6kh3`                                         |
| Level 3 *Fireball*                                       | `8d6`                           | `8d6`                                                                   |
| Level 3 *Fireball* against Fire Resistance               | `8d6//2`                        | `8d6 // 2`                                                              |
| *Fire Bolt* against Fire Resistance at level 5           | `2d10//2`                       | `2d10 // 2`                                                             |
| Three *Magic Missile* bolts                              | `s3(1d4+1)`                     | `sum of 3 (1d4 + 1)`                                                    |
| Three *Magic Missile* bolts with +2 damage on every bolt | `s3(1d4+3)`                     | `sum of 3 (1d4 + 3)`                                                    |
| Roll an ability score: 4d6, drop lowest                  | `4d6dl`<br>≡ `4d6kh3`           | `4d6 drop lowest`<br>≡ `4d6 keep highest 3`                             |
| Reroll the lowest die, then drop the lowest              | `4d6rrldl`                      | `4d6 reroll lowest then drop lowest`                                    |
| Reroll lowest, drop lowest, then keep highest 2          | `5d6rrldlkh2`                   | `5d6 reroll lowest then drop lowest then keep highest 2`                |
| Multiply two independent 1d4 rolls                       | `p2(1d4)`                       | `product of 2 1d4`                                                      |
